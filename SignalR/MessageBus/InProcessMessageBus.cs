using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.MessageBus
{
    public class InProcessMessageBus : IMessageBus
    {
        private static List<InMemoryMessage> _emptyMessageList = new List<InMemoryMessage>();

        private readonly ConcurrentDictionary<string, LockedList<Action<IList<InMemoryMessage>>>> _waitingTasks =
            new ConcurrentDictionary<string, LockedList<Action<IList<InMemoryMessage>>>>();

        private readonly ConcurrentDictionary<string, LockedList<InMemoryMessage>> _cache =
            new ConcurrentDictionary<string, LockedList<InMemoryMessage>>();

        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        private readonly object _lockDowngradeLock = new object();

        private ulong _lastMessageId = 0;
        private long _gcRunning = 0;

        private readonly Timer _timer;

        private readonly ITraceManager _trace;


        public InProcessMessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<ITraceManager>(), garbageCollectMessages: true)
        {
        }

        public InProcessMessageBus(ITraceManager traceManager, bool garbageCollectMessages)
        {
            _trace = traceManager;

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
                return WaitForMessages(eventKeys, timeoutToken);
            }

            try
            {
                // We need to lock here in case messages are added to the bus while we're reading
                _cacheLock.EnterReadLock();
                ulong uid = UInt64.Parse(id);

                if (uid >= _lastMessageId)
                {
                    // Connection already has the latest message, so start wating
                    _trace.Source.TraceInformation("MessageBus: Connection waiting for new messages from id {0}", id);
                    return WaitForMessages(eventKeys, timeoutToken);
                }

                var messages = eventKeys.SelectMany(key => GetMessagesSince(key, uid));

                if (messages.Any())
                {
                    // Messages already in store greater than last received id so return them
                    _trace.Source.TraceInformation("MessageBus: Connection getting messages from cache from id {0}", id);
                    return TaskAsyncHelper.FromResult(GetMessageResult(messages.OrderBy(msg => msg.Id).ToList()));
                }

                // Wait for new messages
                _trace.Source.TraceInformation("MessageBus: Connection waiting for new messages from id {0}", id);
                return WaitForMessages(eventKeys, timeoutToken);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public Task Send(string eventKey, object value)
        {
            var list = _cache.GetOrAdd(eventKey, _ => new LockedList<InMemoryMessage>());

            InMemoryMessage message = null;

            try
            {
                // Take a write lock here so we ensure messages go into the list in order
                _cacheLock.EnterWriteLock();

                // Only 1 save allowed at a time, to ensure messages are added to the list in order
                message = new InMemoryMessage(eventKey, value, GenerateId());
                _trace.Source.TraceInformation("MessageBus: Saving message {0} with eventKey {1} to cache on AppDomain {2}", message.Id, eventKey, AppDomain.CurrentDomain.Id);
                list.AddWithLock(message);

                // Send to waiting callers.
                // This must be done in the read lock to ensure that messages are sent to waiting
                // connections in the order they were saved so that they always get the correct
                // last message id to resubscribe with.
                Broadcast(eventKey, message);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            return TaskAsyncHelper.Empty;
        }

        private void Broadcast(string eventKey, InMemoryMessage message)
        {
            LockedList<Action<IList<InMemoryMessage>>> callbacks;
            if (_waitingTasks.TryGetValue(eventKey, out callbacks))
            {
                var delegates = callbacks.CopyWithLock();
                var messages = new[] { message };

                if (delegates.Count == 0)
                {
                    return;
                }

                _trace.Source.TraceInformation("MessageBus: Sending message {0} with eventKey {1} to {2} waiting connections", message.Id, eventKey, delegates.Count);

                foreach (var callback in delegates)
                {
                    if (callback != null)
                    {
                        callback.Invoke(messages);
                    }
                }
            }
        }

        protected virtual ulong GenerateId()
        {
            return ++_lastMessageId;
        }

        private IList<InMemoryMessage> GetMessagesSince(string eventKey, ulong id)
        {
            LockedList<InMemoryMessage> list = null;
            _cache.TryGetValue(eventKey, out list);

            if (list == null || list.CountWithLock == 0)
            {
                return _emptyMessageList;
            }

            // Create a snapshot so that we ensure the list isn't modified within this scope
            var snapshot = list.CopyWithLock();

            if (snapshot.Count > 0 && snapshot[0].Id > id)
            {
                // All messages in the list are greater than the last message
                return snapshot;
            }

            var index = snapshot.FindLastIndex(msg => msg.Id <= id);

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

        private Task<MessageResult> WaitForMessages(IEnumerable<string> eventKeys, CancellationToken timeoutToken)
        {
            var tcs = new TaskCompletionSource<MessageResult>();
            int callbackCalled = 0;
            Action<IList<InMemoryMessage>> callback = null;

            timeoutToken.Register(() =>
            {
                if (Interlocked.Exchange(ref callbackCalled, 1) == 0)
                {
                    string lastMessageId = _lastMessageId.ToString(CultureInfo.InvariantCulture);
                    tcs.TrySetResult(new MessageResult(lastMessageId, timedOut: true));
                }
            });

            callback = messages =>
            {
                if (Interlocked.Exchange(ref callbackCalled, 1) == 0)
                {
                    tcs.TrySetResult(GetMessageResult(messages));
                }

                // Remove callback for all keys
                foreach (var eventKey in eventKeys)
                {
                    LockedList<Action<IList<InMemoryMessage>>> callbacks;
                    if (_waitingTasks.TryGetValue(eventKey, out callbacks))
                    {
                        callbacks.RemoveWithLock(callback);
                    }
                }
            };

            // Add callback for all keys
            foreach (var eventKey in eventKeys)
            {
                var callbacks = _waitingTasks.GetOrAdd(eventKey, _ => new LockedList<Action<IList<InMemoryMessage>>>());
                callbacks.AddWithLock(callback);
            }

            return tcs.Task;
        }

        private MessageResult GetMessageResult(IList<InMemoryMessage> messages)
        {
            var id = messages[messages.Count - 1].Id;

            return new MessageResult(messages.ToList<Message>(),
                                     id.ToString(CultureInfo.InvariantCulture));
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
                    var messages = entry.Value.CopyWithLock();

                    foreach (var item in messages)
                    {
                        if (item.Expired)
                        {
                            entry.Value.RemoveWithLock(item);
                        }
                    }
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