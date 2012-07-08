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
        private readonly INewMessageBus _newMessageBus;
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
            _newMessageBus = newMessageBus;
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
            var wrappedValue = new WrappedValue(PreprocessValue(value), _serializer);
            return _newMessageBus.Publish(_connectionId, key, wrappedValue);
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
                    Cursor = _newMessageBus.GetCursor(command.Value)
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

        public Task<PersistentResponse> ReceiveAsync(CancellationToken timeoutToken)
        {
            Trace.TraceInformation("Waiting for new messages");

            return TaskAsyncHelper.FromResult(new PersistentResponse());
        }

        public Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken timeoutToken)
        {
            Trace.TraceInformation("Waiting for messages from {0}.", messageId);

            return TaskAsyncHelper.FromResult(new PersistentResponse());
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

        private List<object> ProcessResults(MessageResult result)
        {
            var messageValues = new List<object>(result.TotalCount);

            for (int i = 0; i < result.Messages.Count; i++)
            {
                for (int j = result.Messages[i].Offset; j < result.Messages[i].Offset + result.Messages[i].Count; j++)
                {
                    Message message = result.Messages[i].Array[j];
                    if (SignalCommand.IsCommand(message))
                    {
                        var command = WrappedValue.Unwrap<SignalCommand>(message.Value, _serializer);
                        ProcessCommand(command);
                    }
                    else
                    {
                        messageValues.Add(WrappedValue.Unwrap(message.Value, _serializer));
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
                response.TransportData["Groups"] = _groups;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IDisposable Receive(string messageId, Func<Exception, PersistentResponse, Task<bool>> callback)
        {
            return _newMessageBus.Subscribe(this, messageId, (ex, result) => callback(ex, GetResponse(result)));
        }

        private class GroupData
        {
            public string Name { get; set; }
            public string Cursor { get; set; }
        }
    }
}