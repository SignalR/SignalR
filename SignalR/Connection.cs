using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Connection : IConnection, ITransportConnection
    {
        private readonly IMessageBus _messageBus;
        private readonly IJsonSerializer _serializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly HashSet<string> _groups;
        private readonly ITraceManager _trace;
        private bool _disconnected;
        private bool _aborted;

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
            return Send(_baseSignal, value);
        }

        public virtual Task Send(string signal, object value)
        {
            return SendMessage(signal, value);
        }

        public Task<PersistentResponse> ReceiveAsync(CancellationToken timeoutToken)
        {
            _trace.Source.TraceInformation("Connection: Waiting for new messages");
            Debug.WriteLine("Connection: Waiting for new messages.");

            return _messageBus.GetMessages(Signals, null, timeoutToken)
                              .Then(result => GetResponse(result));
        }

        public Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken timeoutToken)
        {
            _trace.Source.TraceInformation("Connection: Waiting for messages from {0}.", messageId);
            Debug.WriteLine("Connection: Waiting for messages from {0}.", (object)messageId);

            return _messageBus.GetMessages(Signals, messageId, timeoutToken)
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
                Aborted = _aborted,
                TimedOut = result.TimedOut
            };

            PopulateResponseState(response);

            _trace.Source.TraceInformation("Connection: Connection '{0}' received {1} messages, last id {2}", _connectionId, result.Messages.Count, result.LastMessageId);
            Debug.WriteLine("Connection: Connection '{0}' received {1} messages, last id {2}", _connectionId, result.Messages.Count, result.LastMessageId);
            Debug.WriteLine("Connection: Messages");
            Debug.WriteLine(_serializer.Stringify(result.Messages));
            Debug.WriteLine("Connection: Response");
            Debug.WriteLine(_serializer.Stringify(response));

            return response;
        }

        private List<object> ProcessResults(IList<Message> source)
        {
            var messageValues = new List<object>();
            foreach (var message in source)
            {
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
                case CommandType.Abort:
                    _aborted = true;
                    break;
            }
        }

        private Task SendMessage(string key, object value)
        {
            var wrappedValue = new WrappedValue(value, _serializer);
            return _messageBus.Send(_connectionId, key, wrappedValue).Catch();
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