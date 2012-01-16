using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Connection : IConnection, IReceivingConnection
    {
        private readonly Signaler _signaler;
        private readonly IMessageStore _store;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly HashSet<string> _groups;
        private readonly object _lockObj = new object();
        private bool _disconnected;

        public Connection(IMessageStore store,
                          IJsonSerializer jsonSerializer,
                          Signaler signaler,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals)
            : this(store,
                   jsonSerializer,
                   signaler,
                   baseSignal,
                   connectionId,
                   signals,
                   Enumerable.Empty<string>())
        {
        }

        public Connection(IMessageStore store,
                          IJsonSerializer jsonSerializer,
                          Signaler signaler,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals,
                          IEnumerable<string> groups)
        {
            _store = store;
            _jsonSerializer = jsonSerializer;
            _signaler = signaler;
            _baseSignal = baseSignal;
            _connectionId = connectionId;
            _signals = new HashSet<string>(signals);
            _groups = new HashSet<string>(groups);
        }

        // These static events are used for performance monitoring
        public static event EventHandler WaitingForSignal;
        public static event EventHandler MessagesPending;

        public TimeSpan ReceiveTimeout
        {
            get
            {
                return _signaler.DefaultTimeout;
            }
            set
            {
                _signaler.DefaultTimeout = value;
            }
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
            // Get the last message id then wait for new messages to arrive
            return _store.GetLastId()
                         .Then(id => WaitForSignal(id))
                         .FastUnwrap();
        }

        public Task<PersistentResponse> ReceiveAsync(long messageId)
        {
            // Get all messages for this message id, or wait until new messages if there are none
            return GetResponse(messageId)
                .Then((t, id) => ProcessReceive(t, id), messageId)
                .FastUnwrap();
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
            return new Connection(DependencyResolver.Resolve<IMessageStore>(),
                                  DependencyResolver.Resolve<IJsonSerializer>(),
                                  DependencyResolver.Resolve<Signaler>(),
                                  connectionType,
                                  null,
                                  new[] { connectionType });
        }

        private Task<PersistentResponse> ProcessReceive(Task<PersistentResponse> responseTask, long? messageId = null)
        {
            // No messages to return so we need to subscribe until we have something
            if (responseTask.Result == null)
            {
                // There are no messages pending, so wait for a signal
                return WaitForSignal(messageId);
            }

            // There were messages in the response, return the task as is
            if (MessagesPending != null)
            {
                MessagesPending(this, EventArgs.Empty);
            }
            return responseTask;
        }

        private Task<PersistentResponse> WaitForSignal(long? messageId = null)
        {
            if (WaitingForSignal != null)
            {
                WaitingForSignal(this, EventArgs.Empty);
            }

            // Wait for a signal to get triggered and return with a response
            return _signaler.Subscribe(Signals)
                            .Then((result, id) => ProcessSignal(result, id), messageId)
                            .FastUnwrap();
        }

        private Task<PersistentResponse> ProcessSignal(SignalResult result, long? messageId = null)
        {
            if (result.TimedOut)
            {
                PersistentResponse response = GetEmptyResponse(messageId, result.TimedOut);

                // Return a task wrapping the result
                return TaskAsyncHelper.FromResult(response);
            }

            // Get the response for this message id
            return GetResponse(messageId ?? 0)
                .Then<PersistentResponse, long?>((response, id) => response ?? GetEmptyResponse(id), messageId);
        }

        private PersistentResponse GetEmptyResponse(long? messageId, bool timedOut = false)
        {
            var response = new PersistentResponse
            {
                MessageId = messageId ?? 0,
                TimedOut = timedOut
            };

            PopulateResponseState(response);

            return response;
        }

        private Task<PersistentResponse> GetResponse(long messageId)
        {
            // Get all messages for the current set of signals
            return GetMessages(messageId, Signals)
                .Then<List<Message>, long, PersistentResponse>((messages, id) =>
                {
                    if (!messages.Any())
                    {
                        // No messages, not even commands
                        return null;
                    }

                    // Get last message ID
                    messageId = messages[messages.Count - 1].Id;

                    // Do a single sweep through the results to process commands and extract values
                    var messageValues = ProcessResults(messages);

                    var response = new PersistentResponse
                    {
                        MessageId = messageId,
                        Messages = messageValues,
                        Disconnect = _disconnected
                    };

                    PopulateResponseState(response);

                    return response;
                }, messageId);
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
            return _store.Save(key, value)
                         .Then(k => _signaler.Signal(k), key)
                         .FastUnwrap()
                         .Catch();
        }

        private Task<List<Message>> GetMessages(long id, IEnumerable<string> signals)
        {
            return _store.GetAllSince(signals, id)
                .Then(messages => messages.ToList());
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