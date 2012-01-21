using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Connection : IConnection, IReceivingConnection
    {
        private readonly IMessageBus _messageBus;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly HashSet<string> _groups;
        private readonly object _lockObj = new object();
        private bool _disconnected;
        private readonly ITraceManager _trace;

        public Connection(IMessageBus messageBus,
                          IJsonSerializer jsonSerializer,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals,
                          ITraceManager traceManager)
            : this(messageBus,
                   jsonSerializer,
                   baseSignal,
                   connectionId,
                   signals,
                   Enumerable.Empty<string>(),
                   traceManager)
        {
        }

        public Connection(IMessageBus messageBus,
                          IJsonSerializer jsonSerializer,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals,
                          IEnumerable<string> groups,
                          ITraceManager traceManager)
        {
            _messageBus = messageBus;
            _jsonSerializer = jsonSerializer;
            _baseSignal = baseSignal;
            _connectionId = connectionId;
            _signals = new HashSet<string>(signals);
            _groups = new HashSet<string>(groups);
            _trace = traceManager;
        }

        // These static events are used for performance monitoring
        public static event EventHandler WaitingForSignal;
        public static event EventHandler MessagesPending;

        public TimeSpan ReceiveTimeout
        {
            get;
            set;
        }

        private IEnumerable<string> Signals
        {
            get
            {
                return _signals.Concat(_groups);
            }
        }

        public virtual Task Broadcast(object value)
        {
            return Broadcast(_baseSignal, value);
        }

        public virtual Task Broadcast(string key, object value)
        {
            return SendMessage(key, value);
        }

        public Task Send(object value)
        {
            return SendMessage(_connectionId, value);
        }

        public Task<PersistentResponse> ReceiveAsync()
        {
            return _messageBus.GetMessagesSince(Signals)
                .Then(messages => GetResponse(messages.ToList()));
        }

        public Task<PersistentResponse> ReceiveAsync(ulong messageId)
        {            
            return _messageBus.GetMessagesSince(Signals, messageId)
                .Then(messages => GetResponse(messages.ToList()));
        }

        public Task SendCommand(SignalCommand command)
        {
            return SendMessage(SignalCommand.AddCommandSuffix(_connectionId), command);
        }

        public static IConnection GetConnection<T>() where T : PersistentConnection
        {
            return GetConnection(typeof(T).FullName);
        }

        public static IConnection GetConnection(string connectionType)
        {
            return new Connection(DependencyResolver.Resolve<IMessageBus>(),
                                  DependencyResolver.Resolve<IJsonSerializer>(),
                                  connectionType,
                                  null,
                                  new[] { connectionType },
                                  DependencyResolver.Resolve<ITraceManager>());
        }

        private PersistentResponse GetEmptyResponse(ulong? messageId, bool timedOut = false)
        {
            var response = new PersistentResponse
            {
                MessageId = messageId,
                TimedOut = timedOut
            };

            PopulateResponseState(response);

            return response;
        }

        private PersistentResponse GetResponse(List<Message> messages)
        {
            if (!messages.Any())
            {
                // No messages, not even commands
                return null;
            }

            // Get last message ID
            var messageId = messages[messages.Count - 1].Id;

            // Do a single sweep through the results to process commands and extract values
            var messageValues = ProcessResults(messages);

            var response = new PersistentResponse
            {
                MessageId = messageId,
                Messages = messageValues,
                Disconnect = _disconnected
            };

            PopulateResponseState(response);

            _trace.Source.TraceInformation("Connection: Connection {0} received {1} messages, last id {2}", _connectionId, messages.Count, messageId);

            return response;
        }

        private List<object> ProcessResults(List<Message> source)
        {
            var messageValues = new List<object>();
            foreach (var message in source)
            {
                if (SignalCommand.IsCommand(message))
                {
                    ProcessCommand(message);
                }
                else
                {
                    messageValues.Add(message.Value);
                }
            }
            return messageValues;
        }

        private void ProcessCommand(Message message)
        {
            var command = message.GetCommand();
            if (command == null)
            {
                return;
            }

            switch (command.Type)
            {
                case CommandType.AddToGroup:
                    _groups.Add((string)command.Value);
                    break;
                case CommandType.RemoveFromGroup:
                    _groups.Remove((string)command.Value);
                    break;
                case CommandType.Disconnect:
                    _disconnected = true;
                    break;
            }
        }

        private Task SendMessage(string key, object value)
        {
            return _messageBus.Send(key, value).Catch();            
        }

        private void PopulateResponseState(PersistentResponse response)
        {
            // Set the groups on the outgoing transport data
            if (_groups.Any())
            {
                response.TransportData["Groups"] = _groups;
            }
            if (_disconnected)
            {
                response.TransportData["Disconnected"] = true;
            }
        }
    }
}