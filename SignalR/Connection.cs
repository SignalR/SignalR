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
        private readonly HashSet<string> _groups;
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
            _groups = new HashSet<string>(groups);
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

        private IEnumerable<string> Signals
        {
            get
            {
                return _signals.Concat(_groups);
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
            var serializedValue = _serializer.Stringify(PreprocessValue(value));
            return _bus.Publish(_connectionId, key, serializedValue);
        }

        private object PreprocessValue(object value)
        {
            // If this isn't a command then ignore it
            var command = value as SignalCommand;
            if (command == null)
            {
                return value;
            }

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

        public Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken cancel, int messageBufferSize)
        {
            var tcs = new TaskCompletionSource<PersistentResponse>();
            IDisposable subscription = null;

            CancellationTokenRegistration registration = cancel.Register(() =>
            {
                if (subscription != null)
                {
                    subscription.Dispose();
                }
            });

            subscription = _bus.Subscribe(this, messageId, result =>
            {
                PersistentResponse response = GetResponse(result);
                tcs.TrySetResult(response);

                registration.Dispose();

                if (subscription != null)
                {
                    subscription.Dispose();
                }

                return TaskAsyncHelper.False;
            },
            messageBufferSize);

            return tcs.Task;
        }

        public IDisposable Receive(string messageId, Func<PersistentResponse, Task<bool>> callback, int messageBufferSize)
        {
            return _bus.Subscribe(this, messageId, result => callback(GetResponse(result)), messageBufferSize);
        }

        private PersistentResponse GetResponse(MessageResult result)
        {
            // Do a single sweep through the results to process commands and extract values
            var messageValues = ProcessResults(result);

            var response = new PersistentResponse
            {
                MessageId = result.LastMessageId,
                Messages = messageValues,
                Disconnect = _disconnected,
                Aborted = _aborted
            };

            PopulateResponseState(response);

            return response;
        }

        private List<string> ProcessResults(MessageResult result)
        {
            var messageValues = new List<string>(result.TotalCount);

            for (int i = 0; i < result.Messages.Count; i++)
            {
                for (int j = result.Messages[i].Offset; j < result.Messages[i].Offset + result.Messages[i].Count; j++)
                {
                    Message message = result.Messages[i].Array[j];
                    if (SignalCommand.IsCommand(message))
                    {
                        var command = _serializer.Parse<SignalCommand>(message.Value);
                        ProcessCommand(command);
                    }
                    else
                    {
                        messageValues.Add(message.Value);
                    }
                }
            }

            return messageValues;
        }

        private void ProcessCommand(SignalCommand command)
        {
            switch (command.Type)
            {
                case CommandType.AddToGroup:
                    {
                        var groupData = _serializer.Parse<GroupData>(command.Value);

                        if (EventAdded != null)
                        {
                            EventAdded(groupData.Name, groupData.Cursor);
                        }
                    }
                    break;
                case CommandType.RemoveFromGroup:
                    {
                        var groupData = _serializer.Parse<GroupData>(command.Value);

                        if (EventRemoved != null)
                        {
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
                response.TransportData["Groups"] = _groups;
            }
        }

        private class GroupData
        {
            public string Name { get; set; }
            public string Cursor { get; set; }
        }
    }
}