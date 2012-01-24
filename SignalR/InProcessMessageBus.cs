using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class InProcessMessageBus : IMessageBus
    {
        private static List<Message> _emptyMessageList = new List<Message>();

        private readonly ConcurrentDictionary<string, LockedList<Action<IList<Message>>>> _waitingTasks =
            new ConcurrentDictionary<string, LockedList<Action<IList<Message>>>>();

        private readonly ConcurrentDictionary<string, LockedList<Message>> _cache =
            new ConcurrentDictionary<string, LockedList<Message>>();

        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        private readonly object _lockDowngradeLock = new object();

        private ulong _lastMessageId = 0;
        private long _gcRunning = 0;

        private readonly Timer _timer;

        private readonly ITraceManager _trace;

        public InProcessMessageBus()
            : this(DependencyResolver.Resolve<ITraceManager>(), garbageCollectMessages: true)
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

        public Task<IList<Message>> GetMessagesSince(IEnumerable<string> eventKeys, ulong? id = null)
        {
            if (id == null)
            {
                // Wait for new messages
                _trace.Source.TraceInformation("MessageBus: New connection waiting for messages");
                return WaitForMessages(eventKeys);
            }

            try
            {
                // We need to lock here in case messages are added to the bus while we're reading
                _cacheLock.EnterReadLock();

                if (id.Value >= _lastMessageId)
                {
                    // Connection already has the latest message, so start wating
                    _trace.Source.TraceInformation("MessageBus: Connection waiting for new messages from id {0}", id.Value);
                    return WaitForMessages(eventKeys);
                }

                var messages = eventKeys.SelectMany(key => GetMessagesSince(key, id.Value));

                if (messages.Any())
                {
                    // Messages already in store greater than last received id so return them
                    _trace.Source.TraceInformation("MessageBus: Connection getting messages from cache from id {0}", id.Value);
                    return TaskAsyncHelper.FromResult<IList<Message>>(messages.OrderBy(msg => msg.Id).ToList());
                }

                // Wait for new messages
                _trace.Source.TraceInformation("MessageBus: Connection waiting for new messages from id {0}", id.Value);
                return WaitForMessages(eventKeys);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public Task Send(string eventKey, object value)
        {
            var list = _cache.GetOrAdd(eventKey, _ => new LockedList<Message>());

            Message message = null;

            try
            {
                // Take a write lock here so we ensure messages go into the list in order
                _cacheLock.EnterWriteLock();

                // Only 1 save allowed at a time, to ensure messages are added to the list in order
                message = new Message(eventKey, GenerateId(), value);
                _trace.Source.TraceInformation("MessageBus: Saving message {0} to cache", message.Id);
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

        private void Broadcast(string eventKey, Message message)
        {
            LockedList<Action<IList<Message>>> callbacks;
            if (_waitingTasks.TryGetValue(eventKey, out callbacks))
            {
                var delegates = callbacks.CopyWithLock();
                var messages = new[] { message };

                _trace.Source.TraceInformation("MessageBus: Sending message {0} to {1} waiting connections", message.Id, delegates.Count);

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

        private IList<Message> GetMessagesSince(string eventKey, ulong id)
        {
            LockedList<Message> list = null;
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

        private Task<IList<Message>> WaitForMessages(IEnumerable<string> eventKeys)
        {
            var tcs = new TaskCompletionSource<IList<Message>>();
            int callbackCalled = 0;
            Action<IList<Message>> callback = null;

            callback = messages =>
            {
                if (Interlocked.Exchange(ref callbackCalled, 1) == 0)
                {
                    tcs.SetResult(messages);
                }

                // Remove callback for all keys
                foreach (var eventKey in eventKeys)
                {
                    LockedList<Action<IList<Message>>> callbacks;
                    if (_waitingTasks.TryGetValue(eventKey, out callbacks))
                    {
                        callbacks.RemoveWithLock(callback);
                    }
                }
            };

            // Add callback for all keys
            foreach (var eventKey in eventKeys)
            {
                var callbacks = _waitingTasks.GetOrAdd(eventKey, _ => new LockedList<Action<IList<Message>>>());
                callbacks.AddWithLock(callback);
            }

            return tcs.Task;
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