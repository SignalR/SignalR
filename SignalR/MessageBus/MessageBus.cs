using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageBus : MessageBus<ulong>
    {
        public MessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<ITraceManager>(),
                   garbageCollectMessages: true)
        {
        }

        public MessageBus(ITraceManager trace, bool garbageCollectMessages)
            : base(trace,
                   garbageCollectMessages,
                   new DefaultIdGenerator())
        {
        }

        private class DefaultIdGenerator : IIdGenerator<ulong>
        {
            private ulong _id;
            public ulong GetNext()
            {
                return ++_id;
            }

            public ulong ConvertFromString(string value)
            {
                return UInt64.Parse(value, CultureInfo.InvariantCulture);
            }

            public string ConvertToString(ulong value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageBus<T> : INewMessageBus where T : IComparable<T>
    {
        private readonly LockedList<Subscription<T>> _subscriptions = new LockedList<Subscription<T>>();

        private readonly ConcurrentDictionary<string, LockedList<InMemoryMessage<T>>> _cache =
            new ConcurrentDictionary<string, LockedList<InMemoryMessage<T>>>();

        private static List<InMemoryMessage<T>> _emptyMessageList = new List<InMemoryMessage<T>>();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private readonly IIdGenerator<T> _idGenerator;

        private int _workerRunning;

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);
        private long _gcRunning = 0;
        private readonly Timer _timer;
        private readonly ITraceManager _trace;
        private T _lastMessageId;
        private bool _isMessageIdSet;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        /// <param name="idGenerator"></param>
        public MessageBus(IDependencyResolver resolver, IIdGenerator<T> idGenerator)
            : this(resolver.Resolve<ITraceManager>(),
                   garbageCollectMessages: true,
                   idGenerator: idGenerator)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traceManager"></param>
        /// <param name="garbageCollectMessages"></param>
        /// <param name="idGenerator"></param>
        public MessageBus(ITraceManager traceManager, bool garbageCollectMessages, IIdGenerator<T> idGenerator)
        {
            _trace = traceManager;
            _idGenerator = idGenerator;

            if (garbageCollectMessages)
            {
                _timer = new Timer(RemoveExpiredEntries, null, _cleanupInterval, _cleanupInterval);
            }
        }

        private TraceSource Trace
        {
            get
            {
                return _trace["SignalR.InProcessMessageBus"];
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
            var list = _cache.GetOrAdd(eventKey, _ => new LockedList<InMemoryMessage<T>>());

            try
            {
                // Take a write lock here so we ensure messages go into the list in order
                _cacheLock.EnterWriteLock();

                T id = _idGenerator.GetNext();
                _lastMessageId = id;

                // Only 1 save allowed at a time, to ensure messages are added to the list in order
                list.AddWithLock(new InMemoryMessage<T>(eventKey, value, id));
                DoWork();
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
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
            var subscription = new Subscription<T>
            {
                MessageId = messageId == null ? _lastMessageId : _idGenerator.ConvertFromString(messageId),
                Keys = keys.ToArray(),
                Callback = callback
            };

            _subscriptions.AddWithLock(subscription);
            return new DisposableAction(() => _subscriptions.RemoveWithLock(subscription));
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
                    while (_cache.Any())
                    {
                        for (int i = 0; i < _subscriptions.CountWithLock; i++)
                        {
                            var subscription = _subscriptions[i];
                            foreach (var key in subscription.Keys)
                            {
                                var messages = GetMessagesSince(key, subscription.MessageId);
                                if (messages.Count > 0)
                                {
                                    var result = GetMessageResult(messages);
                                    subscription.MessageId = messages[messages.Count - 1].Id;
                                    subscription.Callback.Invoke(null, result);
                                }
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

        private MessageResult GetMessageResult(IList<InMemoryMessage<T>> messages)
        {
            var id = messages[messages.Count - 1].Id;

            return new MessageResult(messages.ToList<Message>(), _idGenerator.ConvertToString(id));
        }

        private IList<InMemoryMessage<T>> GetMessagesSince(string eventKey, T id)
        {
            LockedList<InMemoryMessage<T>> list = null;
            _cache.TryGetValue(eventKey, out list);

            if (list == null || list.CountWithLock == 0)
            {
                return _emptyMessageList;
            }

            // Create a snapshot so that we ensure the list isn't modified within this scope
            var snapshot = list.CopyWithLock();

            if (snapshot.Count > 0 && snapshot[0].Id.CompareTo(id) > 0)
            {
                // All messages in the list are greater than the last message
                return snapshot;
            }

            var index = snapshot.FindLastIndex(msg => msg.Id.CompareTo(id) <= 0);

            if (index < 0)
            {
                return _emptyMessageList;
            }

            var startIndex = index + 1;

            if (startIndex >= snapshot.Count)
            {
                return _emptyMessageList;
            }

            return snapshot.GetRange(startIndex, snapshot.Count - startIndex);
        }

        private void RemoveExpiredEntries(object state)
        {
            if (Interlocked.Exchange(ref _gcRunning, 1) == 1 || Debugger.IsAttached)
            {
                return;
            }

            try
            {
                // Take a snapshot of the entries
                var entries = _cache.ToList();

                // Remove all the expired ones
                foreach (var entry in entries)
                {
                    entry.Value.RemoveWithLock(item => item.Expired);
                }
            }
            catch (Exception ex)
            {
                // Exception on bg thread, bad! Log and swallow to stop the process exploding
                Trace.TraceInformation("Error during InProcessMessageStore clean up on background thread: {0}", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _gcRunning, 0);
            }
        }
        
        internal class Subscription<TValue>
        {
            public string[] Keys;
            public TValue MessageId;
            public Action<Exception, MessageResult> Callback;
        }
    }
}
