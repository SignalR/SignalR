using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class InProcessMessageBus : InProcessMessageBus<ulong>
    {
        public InProcessMessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<ITraceManager>(),
                   garbageCollectMessages: true)
        {
        }

        public InProcessMessageBus(ITraceManager trace, bool garbageCollectMessages)
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

    public class InProcessMessageBus<T> : IMessageBus where T : IComparable<T>
    {
        private static List<InMemoryMessage<T>> _emptyMessageList = new List<InMemoryMessage<T>>();

        private readonly ConcurrentDictionary<string, LockedList<Action<IList<InMemoryMessage<T>>>>> _waitingTasks =
            new ConcurrentDictionary<string, LockedList<Action<IList<InMemoryMessage<T>>>>>();

        private readonly ConcurrentDictionary<string, LockedList<InMemoryMessage<T>>> _cache =
            new ConcurrentDictionary<string, LockedList<InMemoryMessage<T>>>();

        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        private readonly IIdGenerator<T> _idGenerator;

        private T _lastMessageId;
        private long _gcRunning = 0;

        private readonly Timer _timer;

        private readonly ITraceManager _trace;

        public InProcessMessageBus(IDependencyResolver resolver, IIdGenerator<T> idGenerator)
            : this(resolver.Resolve<ITraceManager>(),
                   garbageCollectMessages: true,
                   idGenerator: idGenerator)
        {

        }

        public InProcessMessageBus(ITraceManager traceManager, bool garbageCollectMessages, IIdGenerator<T> idGenerator)
        {
            _trace = traceManager;
            _idGenerator = idGenerator;

            if (garbageCollectMessages)
            {
                _timer = new Timer(RemoveExpiredEntries, null, _cleanupInterval, _cleanupInterval);
            }
        }

        public Task<MessageResult> GetMessages(IEnumerable<string> eventKeys, string id, CancellationToken timeoutToken)
        {
            if (String.IsNullOrEmpty(id))
            {
                // Wait for new messages
                _trace.Source.TraceInformation("MessageBus: New connection waiting for messages");
                return WaitForMessages(eventKeys, timeoutToken, default(T));
            }

            try
            {
                // We need to lock here in case messages are added to the bus while we're reading
                _cacheLock.EnterReadLock();
                T uuid = _idGenerator.ConvertFromString(id);

                if (uuid.CompareTo(_lastMessageId) > 0)
                {
                    // BUG 24: Connection already has the latest message, so reset the id
                    // This can happen if the server is reset (appdomain or entire server incase of self host)
                    _trace.Source.TraceInformation("MessageBus: Connection asking for message id {0} when the largest is {1}. Resetting id", id, _lastMessageId);
                    uuid = default(T);
                }
                else if (uuid.CompareTo(_lastMessageId) == 0)
                {
                    // Connection already has the latest message, so start wating
                    _trace.Source.TraceInformation("MessageBus: Connection waiting for new messages from id {0}", uuid);
                    return WaitForMessages(eventKeys, timeoutToken, uuid);
                }

                var messages = eventKeys.SelectMany(key => GetMessagesSince(key, uuid));

                if (messages.Any())
                {
                    // Messages already in store greater than last received id so return them
                    _trace.Source.TraceInformation("MessageBus: Connection getting messages from cache from id {0}", uuid);
                    return TaskAsyncHelper.FromResult(GetMessageResult(messages.OrderBy(msg => msg.Id).ToList()));
                }

                // Wait for new messages
                _trace.Source.TraceInformation("MessageBus: Connection waiting for new messages from id {0}", uuid);
                return WaitForMessages(eventKeys, timeoutToken, uuid);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public Task Send(string connectionId, string eventKey, object value)
        {
            var list = _cache.GetOrAdd(eventKey, _ => new LockedList<InMemoryMessage<T>>());

            InMemoryMessage<T> message = null;

            try
            {
                // Take a write lock here so we ensure messages go into the list in order
                _cacheLock.EnterWriteLock();

                // Only 1 save allowed at a time, to ensure messages are added to the list in order
                message = new InMemoryMessage<T>(eventKey, value, GenerateId());
                _trace.Source.TraceInformation("MessageBus: Saving message {0} with eventKey '{1}' to cache on AppDomain {2}", message.Id, eventKey, AppDomain.CurrentDomain.Id);
                list.AddWithLock(message);

                // Send to waiting callers.
                // This must be done in the write lock to ensure that messages are sent to waiting
                // connections in the order they were saved so that they always get the correct
                // last message id to resubscribe with. Moving this outside the lock can enable
                // a subsequent send to overtake the previous send, resulting in the waiting connection
                // getting a last message id that is after the first save, hence missing a message.
                Broadcast(eventKey, message);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            return TaskAsyncHelper.Empty;
        }

        private T GenerateId()
        {
            return _lastMessageId = _idGenerator.GetNext();
        }

        private void Broadcast(string eventKey, InMemoryMessage<T> message)
        {
            LockedList<Action<IList<InMemoryMessage<T>>>> callbacks;
            if (_waitingTasks.TryGetValue(eventKey, out callbacks))
            {
                var delegates = callbacks.CopyWithLock();
                var messages = new[] { message };

                if (delegates.Count == 0)
                {
                    _trace.Source.TraceInformation("MessageBus: Sending message {0} with eventKey '{1}' to 0 waiting connections", message.Id, eventKey);
                    return;
                }

                _trace.Source.TraceInformation("MessageBus: Sending message {0} with eventKey '{1}' to {2} waiting connections", message.Id, eventKey, delegates.Count);

                foreach (var callback in delegates)
                {
                    if (callback != null)
                    {
                        callback.Invoke(messages);
                    }
                }
            }
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

        private Task<MessageResult> WaitForMessages(IEnumerable<string> eventKeys, CancellationToken timeoutToken, T lastId)
        {
            var tcs = new TaskCompletionSource<MessageResult>();
            int callbackCalled = 0;
            Action<IList<InMemoryMessage<T>>> callback = null;
            CancellationTokenRegistration registration = default(CancellationTokenRegistration);

            registration = timeoutToken.Register(() =>
            {
                try
                {
                    if (Interlocked.Exchange(ref callbackCalled, 1) == 0)
                    {
                        string id = _idGenerator.ConvertToString(_lastMessageId);
                        tcs.TrySetResult(new MessageResult(id, timedOut: true));
                    }

                    // Remove callback for all keys
                    foreach (var eventKey in eventKeys)
                    {
                        LockedList<Action<IList<InMemoryMessage<T>>>> callbacks;
                        if (_waitingTasks.TryGetValue(eventKey, out callbacks))
                        {
                            callbacks.RemoveWithLock(callback);
                        }
                    }
                }
                finally
                {
                    registration.Dispose();
                }
            });

            callback = receivedMessages =>
            {
                try
                {
                    // REVIEW: Consider the case where lastId is a referene type and is null.
                    // What wouls this return? Does it matter?
                    var messages = receivedMessages.Where(m => m.Id.CompareTo(lastId) > 0)
                                                   .ToList();

                    if (messages.Count == 0)
                    {
                        return;
                    }

                    if (Interlocked.Exchange(ref callbackCalled, 1) == 0)
                    {
                        tcs.TrySetResult(GetMessageResult(messages));
                    }

                    // Remove callback for all keys
                    foreach (var eventKey in eventKeys)
                    {
                        LockedList<Action<IList<InMemoryMessage<T>>>> callbacks;
                        if (_waitingTasks.TryGetValue(eventKey, out callbacks))
                        {
                            callbacks.RemoveWithLock(callback);
                        }
                    }
                }
                finally
                {
                    registration.Dispose();
                }
            };

            // Add callback for all keys
            foreach (var eventKey in eventKeys)
            {
                var callbacks = _waitingTasks.GetOrAdd(eventKey, _ => new LockedList<Action<IList<InMemoryMessage<T>>>>());
                callbacks.AddWithLock(callback);
            }

            return tcs.Task;
        }

        private MessageResult GetMessageResult(IList<InMemoryMessage<T>> messages)
        {
            var id = messages[messages.Count - 1].Id;

            return new MessageResult(messages.ToList<Message>(), _idGenerator.ConvertToString(id));
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
                Trace.TraceError("Error during InProcessMessageStore clean up on background thread: {0}", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _gcRunning, 0);
            }
        }
    }
}