using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Connection : IConnection
    {
        private readonly Signaler _signaler;
        private readonly IMessageStore _store;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly HashSet<string> _groups;
        private readonly object _lockObj = new object();

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

        public virtual Task Broadcast(string message, object value)
        {
            return SendMessage(message, value);
        }

        public Task Send(object value)
        {
            return SendMessage(_connectionId, value);
        }

        public Task<PersistentResponse> ReceiveAsync()
        {
            // Get the last message id then wait for new messages to arrive
            return _store.GetLastId()
                         .Then(storeTask => WaitForSignal(storeTask.Result))
                         .FastUnwrap();
        }

        public Task<PersistentResponse> ReceiveAsync(long messageId)
        {
            // Get all messages for this message id, or wait until new messages if there are none
            return GetResponse(messageId).Then(task => ProcessReceive(task, messageId))
                                         .FastUnwrap();
        }

        public static IConnection GetConnection<T>() where T : PersistentConnection
        {
            return GetConnection(typeof(T).FullName);
        }

        public static IConnection GetConnection(string connectionType)
        {
            return new Connection(DependencyResolver.Resolve<IMessageStore>(),
                                  DependencyResolver.Resolve<IJsonSerializer>(),
                                  Signaler.Instance,
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
                if (WaitingForSignal != null)
                {
                    WaitingForSignal(this, EventArgs.Empty);
                }
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
            // Wait for a signal to get triggered and return with a response
            return _signaler.Subscribe(Signals)
                            .Then(task => ProcessSignal(task, messageId))
                            .FastUnwrap();
        }

        private Task<PersistentResponse> ProcessSignal(Task<SignalResult> signalTask, long? messageId = null)
        {
            if (signalTask.Result.TimedOut)
            {
                PersistentResponse response = GetEmptyResponse(messageId);

                // Return a task wrapping the result
                return TaskAsyncHelper.FromResult(response);
            }

            // Get the response for this message id
            return GetResponse(messageId ?? 0)
                .Then(task => task.Result ?? GetEmptyResponse(messageId));
        }

        private PersistentResponse GetEmptyResponse(long? messageId)
        {
            var response = new PersistentResponse
            {
                MessageId = messageId ?? 0
            };

            PopulateResponseState(response);

            return response;
        }

        private Task<PersistentResponse> GetResponse(long messageId)
        {
            // Get all messages for the current set of signals
            return GetMessages(messageId, Signals)
                .Then(messageTask =>
                {
                    var results = messageTask.Result;
                    if (!results.Any())
                    {
                        return null;
                    }

                    // Get last message ID
                    messageId = results[results.Count - 1].Id;

                    // Do a single sweep through the results to process commands and extract values
                    var messageValues = ProcessResults(results);

                    var response = new PersistentResponse
                    {
                        MessageId = messageId,
                        Messages = messageValues
                    };

                    PopulateResponseState(response);

                    return response;
                });
        }

        private List<object> ProcessResults(List<Message> source)
        {
            var messageValues = new List<object>();
            foreach (var message in source)
            {
                if (message.SignalKey.EndsWith(PersistentConnection.SignalrCommand))
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
            var command = GetCommand(message);
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
            }
        }

        private SignalCommand GetCommand(Message message)
        {
            var command = message.Value as SignalCommand;

            // Optimization for in memory message store
            if (command != null)
            {
                return command;
            }

            // Otherwise deserialize the message value
            string value = message.Value as string;
            if (value == null)
            {
                return null;
            }

            return _jsonSerializer.Parse<SignalCommand>(value);
        }

        private Task SendMessage(string message, object value)
        {
            return _store.Save(message, value)
                         .Then(_ => _signaler.Signal(message))
                         .FastUnwrap()
                         .Catch();
        }

        private Task<List<Message>> GetMessages(long id, IEnumerable<string> signals)
        {
            return _store.GetAllSince(signals, id)
                .Then(task => task.Result.ToList());
        }

        private void PopulateResponseState(PersistentResponse response)
        {
            // Set the groups on the outgoing transport data
            response.TransportData["Groups"] = _groups;
        }
    }
}