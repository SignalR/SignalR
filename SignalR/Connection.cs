using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Connection : IConnection, ITransportConnection, ISubscriber
    {
        private readonly IMessageBus _bus;
        private readonly IJsonSerializer _serializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly SafeSet<string> _groups;
        
        private readonly PerformanceCounter _msgsRecTotalCounter;
        private readonly PerformanceCounter _msgsRecPerSecCounter;
        private readonly PerformanceCounter _msgsSentTotalCounter;
        private readonly PerformanceCounter _msgsSentPerSecCounter;

        private bool _disconnected;
        private bool _aborted;
        private readonly Lazy<TraceSource> _traceSource;
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _acks = new ConcurrentDictionary<string, TaskCompletionSource<object>>();

        public Connection(IMessageBus newMessageBus,
                          IJsonSerializer jsonSerializer,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals,
                          IEnumerable<string> groups,
                          ITraceManager traceManager,
                          IPerformanceCounterWriter performanceCounterWriter)
        {
            _bus = newMessageBus;
            _serializer = jsonSerializer;
            _baseSignal = baseSignal;
            _connectionId = connectionId;
            _signals = new HashSet<string>(signals);
            _groups = new SafeSet<string>(groups);
            _traceSource = new Lazy<TraceSource>(() => traceManager["SignalR.Connection"]);
            
            var counters = performanceCounterWriter;
            _msgsRecTotalCounter = counters.GetCounter(PerformanceCounters.ConnectionMessagesReceivedTotal);
            _msgsRecPerSecCounter = counters.GetCounter(PerformanceCounters.ConnectionMessagesReceivedPerSec);
            _msgsSentTotalCounter = counters.GetCounter(PerformanceCounters.ConnectionMessagesSentTotal);
            _msgsSentPerSecCounter = counters.GetCounter(PerformanceCounters.ConnectionMessagesSentPerSec);
        }

        IEnumerable<string> ISubscriber.EventKeys
        {
            get
            {
                return Signals;
            }
        }

        public event Action<string> EventAdded;

        public event Action<string> EventRemoved;

        public string Identity
        {
            get
            {
                return _connectionId;
            }
        }

        private IEnumerable<string> Signals
        {
            get
            {
                return _signals.Concat(_groups.GetSnapshot());
            }
        }

        private TraceSource Trace
        {
            get
            {
                return _traceSource.Value;
            }
        }

        public virtual Task Broadcast(object value)
        {
            return Send(_baseSignal, value);
        }

        public virtual Task Send(string signal, object value)
        {
            return SendMessage(signal, value);
        }

        private Task SendMessage(string key, object value)
        {
            Message message = CreateMessage(key, value);
            _msgsSentTotalCounter.SafeIncrement();
            _msgsSentPerSecCounter.SafeIncrement();

            if (message.WaitForAck)
            {
                Task ackTask = _acks.GetOrAdd(message.CommandId, _ => new TaskCompletionSource<object>()).Task;
                return _bus.Publish(message).Then(task => task, ackTask);
            }

            return _bus.Publish(message);
        }

        private Message CreateMessage(string key, object value)
        {
            var command = value as Command;
            var message = new Message(_connectionId, key, _serializer.Stringify(value));

            if (command != null)
            {
                // Set the command id
                message.CommandId = command.Id;
                message.WaitForAck = command.WaitForAck;
            }

            return message;
        }

        public Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken cancel, int maxMessages)
        {
            return _bus.ReceiveAsync<PersistentResponse>(this, messageId, cancel, maxMessages, GetResponse, (result, response) =>
            {
                response.MessageId = result.LastMessageId;
            });
        }
        public IDisposable Receive(string messageId, Func<PersistentResponse, Task<bool>> callback, int maxMessages)
        {
            return _bus.Subscribe(this, messageId, result =>
            {
                Task<bool> keepGoing = callback(GetResponse(result));
                if (result.Terminal)
                {
                    keepGoing = TaskAsyncHelper.False;
                }
                return keepGoing;
            },
            maxMessages);
        }

        private PersistentResponse GetResponse(MessageResult result)
        {
            // Do a single sweep through the results to process commands and extract values
            ProcessResults(result);

            var response = new PersistentResponse
            {
                MessageId = result.LastMessageId,
                Messages = result.Messages,
                Disconnect = _disconnected,
                Aborted = _aborted,
                TotalCount = result.TotalCount
            };

            PopulateResponseState(response);

            _msgsRecTotalCounter.SafeIncrementBy(result.TotalCount);
            _msgsRecPerSecCounter.SafeIncrementBy(result.TotalCount);

            return response;
        }

        private void ProcessResults(MessageResult result)
        {
            result.Messages.Enumerate(message => message.IsCommand || message.IsAck,
                                      message =>
                                      {
                                          if (message.IsAck)
                                          {
                                              TaskCompletionSource<object> tcs;
                                              if (_acks.TryRemove(message.CommandId, out tcs))
                                              {
                                                  tcs.TrySetResult(null);
                                              }
                                          }
                                          else
                                          {
                                              var command = _serializer.Parse<Command>(message.Value);
                                              ProcessCommand(command);

                                              // Only send the ack if this command is waiting for it
                                              if (message.WaitForAck)
                                              {
                                                  // If we're on the same box and there's a pending ack for this command then
                                                  // just trip it
                                                  TaskCompletionSource<object> tcs;
                                                  if (_acks.TryRemove(message.CommandId, out tcs))
                                                  {
                                                      tcs.TrySetResult(null);
                                                  }
                                                  else
                                                  {
                                                      // Send a message through the bus confirming that we got the message
                                                      // REVIEW: Do we retry if this fails?
                                                      _bus.Ack(_connectionId, message.Key, message.CommandId).Catch();
                                                  }
                                              }
                                          }
                                      });
        }

        private void ProcessCommand(Command command)
        {
            switch (command.Type)
            {
                case CommandType.AddToGroup:
                    {
                        var name = command.Value;

                        if (EventAdded != null)
                        {
                            _groups.Add(name);
                            EventAdded(name);
                        }
                    }
                    break;
                case CommandType.RemoveFromGroup:
                    {
                        var name = command.Value;

                        if (EventRemoved != null)
                        {
                            _groups.Remove(name);
                            EventRemoved(name);
                        }
                    }
                    break;
                case CommandType.Disconnect:
                    _disconnected = true;
                    break;
                case CommandType.Abort:
                    _aborted = true;
                    break;
            }
        }

        private void PopulateResponseState(PersistentResponse response)
        {
            // Set the groups on the outgoing transport data
            if (_groups.Count > 0)
            {
                if (response.TransportData == null)
                {
                    response.TransportData = new Dictionary<string, object>();
                }

                response.TransportData["Groups"] = _groups.GetSnapshot();
            }
        }
    }
}