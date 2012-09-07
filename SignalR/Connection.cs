using System;
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
        private readonly INewMessageBus _bus;
        private readonly IJsonSerializer _serializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly SafeSet<string> _groups;
        private bool _disconnected;
        private bool _aborted;
        private readonly Lazy<TraceSource> _traceSource;

        public Connection(INewMessageBus newMessageBus,
                          IJsonSerializer jsonSerializer,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals,
                          IEnumerable<string> groups,
                          ITraceManager traceManager)
        {
            _bus = newMessageBus;
            _serializer = jsonSerializer;
            _baseSignal = baseSignal;
            _connectionId = connectionId;
            _signals = new HashSet<string>(signals);
            _groups = new SafeSet<string>(groups);
            _traceSource = new Lazy<TraceSource>(() => traceManager["SignalR.Connection"]);
        }

        IEnumerable<string> ISubscriber.EventKeys
        {
            get
            {
                return Signals;
            }
        }

        public event Action<string, string> EventAdded;

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
            return _bus.Publish(message);
        }

        private Message CreateMessage(string key, object value)
        {
            bool isCommand;
            value = PreprocessValue(value, out isCommand);
            var serializedValue = _serializer.Stringify(value);

            return new Message(_connectionId, key, serializedValue)
            {
                IsCommand = isCommand
            };
        }

        private object PreprocessValue(object value, out bool isCommand)
        {
            isCommand = false;

            // If this isn't a command then ignore it
            var command = value as Command;
            if (command == null)
            {
                return value;
            }

            isCommand = true;

            if (command.Type == CommandType.AddToGroup)
            {
                var group = new GroupData
                {
                    Name = command.Value,
                    Cursor = _bus.GetCursor(command.Value)
                };

                command.Value = _serializer.Stringify(group);
            }
            else if (command.Type == CommandType.RemoveFromGroup)
            {
                var group = new GroupData
                {
                    Name = command.Value
                };

                command.Value = _serializer.Stringify(group);
            }

            return command;
        }

        public Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken cancel, int maxMessages)
        {
            var tcs = new TaskCompletionSource<PersistentResponse>();
            IDisposable subscription = null;
            var wh = new ManualResetEventSlim(initialState: false);

            CancellationTokenRegistration registration = cancel.Register(() =>
            {
                wh.Wait();
                subscription.Dispose();
            });

            PersistentResponse response = null;

            subscription = _bus.Subscribe(this, messageId, result =>
            {
                wh.Wait();

                if (Interlocked.CompareExchange(ref response, GetResponse(result), null) == null)
                {
                    registration.Dispose();
                    subscription.Dispose();
                }

                if (result.Terminal)
                {
                    // Use the terminal message id since it's the most accurate
                    // This is important for things like manipulating groups
                    // since the message id is only updated after processing the commands
                    // as part of this call itself.
                    response.MessageId = result.LastMessageId;
                    tcs.TrySetResult(response);

                    return TaskAsyncHelper.False;
                }

                return TaskAsyncHelper.True;
            },
            maxMessages);

            // Set this after the subscription is assigned
            wh.Set();

            return tcs.Task;
        }

        public IDisposable Receive(string messageId, Func<PersistentResponse, Task<bool>> callback, int maxMessages)
        {
            return _bus.Subscribe(this, messageId, result => callback(GetResponse(result)), maxMessages);
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

            return response;
        }

        private void ProcessResults(MessageResult result)
        {
            for (int i = 0; i < result.Messages.Count; i++)
            {
                for (int j = result.Messages[i].Offset; j < result.Messages[i].Offset + result.Messages[i].Count; j++)
                {
                    Message message = result.Messages[i].Array[j];
                    if (message.IsCommand)
                    {
                        var command = _serializer.Parse<Command>(message.Value);
                        ProcessCommand(command);
                    }
                }
            }
        }

        private void ProcessCommand(Command command)
        {
            switch (command.Type)
            {
                case CommandType.AddToGroup:
                    {
                        var groupData = _serializer.Parse<GroupData>(command.Value);

                        if (EventAdded != null)
                        {
                            _groups.Add(groupData.Name);
                            EventAdded(groupData.Name, groupData.Cursor);
                        }
                    }
                    break;
                case CommandType.RemoveFromGroup:
                    {
                        var groupData = _serializer.Parse<GroupData>(command.Value);

                        if (EventRemoved != null)
                        {
                            _groups.Remove(groupData.Name);
                            EventRemoved(groupData.Name);
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

        private class GroupData
        {
            public string Name { get; set; }
            public string Cursor { get; set; }
        }
    }
}