using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageBus : INewMessageBus
    {
        private readonly LockedList<Subscription> _subscriptions = new LockedList<Subscription>();

        private readonly ConcurrentDictionary<string, MessageStore<InMemoryMessage>> _cache =
            new ConcurrentDictionary<string, MessageStore<InMemoryMessage>>();

        private static List<InMemoryMessage<ulong>> _emptyMessageList = new List<InMemoryMessage<ulong>>();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();


        private int _workerRunning;

        private readonly ITraceManager _trace;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        public MessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<ITraceManager>())
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traceManager"></param>
        public MessageBus(ITraceManager traceManager)
        {
            _trace = traceManager;
        }

        private TraceSource Trace
        {
            get
            {
                return _trace["SignalR.MessageBus"];
            }
        }

        /// <summary>
        /// Publishes a new message to the specified event on the bus.
        /// </summary>
        /// <param name="source">A value representing the source of the data sent.</param>
        /// <param name="eventKey">The specific event key to send data to.</param>
        /// <param name="value">The value to send.</param>
        public void Publish(string source, string eventKey, object value)
        {
            var list = _cache.GetOrAdd(eventKey, _ => new MessageStore<InMemoryMessage>(100));

            list.Add(new InMemoryMessage(eventKey, value));

            DoWork();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="messageId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IEnumerable<string> keys, string messageId, Action<Exception, MessageResult> callback)
        {
            var subscription = new Subscription
            {
                Cursors = GetCursors(messageId, keys),
                Callback = callback
            };

            _subscriptions.AddWithLock(subscription);
            return new DisposableAction(() => _subscriptions.RemoveWithLock(subscription));
        }

        private Cursor[] GetCursors(string messageId, IEnumerable<string> keys)
        {
            if (messageId == null)
            {
                return keys.Select(key => new Cursor { Key = key, MessageId = GetMessageId(key) }).ToArray();
            }

            return JsonConvert.DeserializeObject<Cursor[]>(messageId);
        }

        private ulong GetMessageId(string key)
        {
            MessageStore<InMemoryMessage> store;
            if (_cache.TryGetValue(key, out store))
            {
                return store.Id + 1;
            }

            return 0;
        }

        private void DoWork()
        {
            if (Interlocked.Exchange(ref _workerRunning, 1) == 0)
            {
                StartWorker();
            }
        }

        private void StartWorker()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (_cache.Values.Any())
                    {
                        for (int i = 0; i < _subscriptions.CountWithLock; i++)
                        {
                            Subscription subscription = _subscriptions[i];
                            var results = new List<ResultSet>();
                            foreach (var cursor in subscription.Cursors)
                            {
                                MessageStore<InMemoryMessage> messages;
                                if (_cache.TryGetValue(cursor.Key, out messages))
                                {
                                    var storeResult = messages.GetMessages(cursor.MessageId);
                                    var result = new ResultSet
                                    {
                                        Cursor = cursor,
                                        StoreResult = storeResult
                                    };

                                    cursor.MessageId = storeResult.FirstMessageId + (ulong)storeResult.Messages.Length;

                                    if (storeResult.Messages.Length > 0)
                                    {
                                        results.Add(result);
                                    }
                                }
                            }

                            if (results.Count > 0)
                            {
                                MessageResult messageResult = GetMessageResult(results);
                                subscription.Callback.Invoke(null, messageResult);
                            }
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _workerRunning, 1);
                }
            });
        }

        private MessageResult GetMessageResult(List<ResultSet> results)
        {
            var messages = results.SelectMany(r => r.StoreResult.Messages).ToArray();
            return new MessageResult(messages, CalculateToken(results));
        }

        private string CalculateToken(List<ResultSet> results)
        {
            return JsonConvert.SerializeObject(results.Select(k => k.Cursor));
        }

        internal class Subscription
        {
            public Cursor[] Cursors;
            public Action<Exception, MessageResult> Callback;
        }

        internal class Cursor
        {
            [JsonProperty(PropertyName = "k")]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "m")]
            public ulong MessageId { get; set; }
        }

        internal class ResultSet
        {
            public Cursor Cursor { get; set; }
            public MessageStoreResult<InMemoryMessage> StoreResult { get; set; }
        }
    }
}
