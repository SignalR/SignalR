using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Infrastructure;
using SignalR.MessageBus;

namespace SignalR
{
    public class Connection : IConnection, IReceivingConnection
    {
        private readonly IMessageBus _messageBus;
        private readonly IJsonSerializer _serializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly HashSet<string> _groups;
        private readonly ITraceManager _trace;
        private bool _disconnected;
        private bool _timedOut;

        public Connection(IMessageBus messageBus,
                          IJsonSerializer jsonSerializer,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals,
                          IEnumerable<string> groups,
                          ITraceManager traceManager)
        {
            _messageBus = messageBus;
            _serializer = jsonSerializer;
            _baseSignal = baseSignal;
            _connectionId = connectionId;
            _signals = new HashSet<string>(signals);
            _groups = new HashSet<string>(groups);
            _trace = traceManager;
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
            return _messageBus.GetMessages(Signals, null)
                .Then(result => GetResponse(result));
        }

        public Task<PersistentResponse> ReceiveAsync(string messageId)
        {
            return _messageBus.GetMessages(Signals, messageId)
                .Then(result => GetResponse(result));
        }

        public Task SendCommand(SignalCommand command)
        {
            return SendMessage(SignalCommand.AddCommandSuffix(_connectionId), command);
        }

        private PersistentResponse GetResponse(MessageResult result)
        {
            // Do a single sweep through the results to process commands and extract values
            var messageValues = ProcessResults(result.Messages);

            var response = new PersistentResponse
            {
                MessageId = result.LastMessageId,
                Messages = messageValues,
                Disconnect = _disconnected,
                TimedOut = _timedOut
            };

            PopulateResponseState(response);

            _trace.Source.TraceInformation("Connection: Connection {0} received {1} messages, last id {2}", _connectionId, result.Messages.Count, result.LastMessageId);

            return response;
        }

        private List<object> ProcessResults(IList<Message> source)
        {
            var messageValues = new List<object>();
            foreach (var message in source)
            {
                SignalCommand command;
                if (SignalCommand.TryGetCommand(message, _serializer, out command))
                {
                    ProcessCommand(command);
                }
                else
                {
                    messageValues.Add(message.Value);
                }
            }
            return messageValues;
        }

        private void ProcessCommand(SignalCommand command)
        {
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
                case CommandType.Timeout:
                    _timedOut = true;
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
        }
    }
}