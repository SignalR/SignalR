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

        private readonly ConcurrentDictionary<string, LockedList<Action<IEnumerable<Message>>>> _waitingTasks =
            new ConcurrentDictionary<string, LockedList<Action<IEnumerable<Message>>>>();

        private readonly ConcurrentDictionary<string, LockedList<Message>> _cache =
            new ConcurrentDictionary<string, LockedList<Message>>();

        //private readonly object _messageCreationLock = new object();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        private ulong _lastMessageId = 0;
        private long _gcRunning = 0;

        private readonly Timer _timer;

        public InProcessMessageBus()
            : this(garbageCollectMessages: true)
        {
        }

        public InProcessMessageBus(bool garbageCollectMessages)
        {
            if (garbageCollectMessages)
            {
                _timer = new Timer(RemoveExpiredEntries, null, _cleanupInterval, _cleanupInterval);
            }
        }

        public Task<IEnumerable<Message>> GetMessagesSince(IEnumerable<string> eventKeys, ulong? id = null)
        {
            if (id == null)
            {
                // Wait for new messages
                return WaitForMessages(eventKeys);
            }

            IEnumerable<Message> messages;
            try
            {
                _cacheLock.EnterReadLock();

                messages = eventKeys.SelectMany(key => GetMessagesSince(key, id.Value));
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            if (messages.Any())
            {
                // Messages already in store greater than last received id so return them
                return TaskAsyncHelper.FromResult((IEnumerable<Message>)messages.OrderBy(msg => msg.Id));
            }

            // Wait for new messages
            return WaitForMessages(eventKeys);
        }

        public Task Send(string eventKey, object value)
        {
            var list = _cache.GetOrAdd(eventKey, _ => new LockedList<Message>());

            Message message = null;
            //lock (_messageCreationLock)
            //{

            try
            {
                _cacheLock.EnterWriteLock();

                // Only 1 save allowed at a time, to ensure messages are added to the list in order
                message = new Message(eventKey, GenerateId(), value);
                list.Add(message);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
            
            //}

            // Send to waiting callers
            LockedList<Action<IEnumerable<Message>>> taskCompletionSources;
            if (_waitingTasks.TryGetValue(eventKey, out taskCompletionSources))
            {
                var delegates = taskCompletionSources.Copy();
                var messages = new[] { message };

                foreach (var callback in delegates)
                {
                    if (callback != null)
                    {
                        callback.Invoke(messages);
                    }
                }
            }

            return TaskAsyncHelper.Empty;
        }

        private ulong GenerateId()
        {
            // do some crazy shit here
            return ++_lastMessageId;
        }

        private IEnumerable<Message> GetMessagesSince(string eventKey, ulong id)
        {
            LockedList<Message> list = null;
            _cache.TryGetValue(eventKey, out list);
            
            if (list == null || list.Count == 0)
            {
                return _emptyMessageList;
            }

            if (list.Count > 0 && list[0].Id > id)
            {
                // All messages in the list are greater than the last message
                return list.List;
            }

            var index = list.FindLastIndexLockFree(msg => msg.Id <= id);

            if (index < 0)
            {
                return _emptyMessageList;
            }

            var startIndex = index + 1;

            if (startIndex >= list.Count)
            {
                return _emptyMessageList;
            }

            return list.GetRangeLockFree(startIndex, list.Count - startIndex);
        }

        private Task<IEnumerable<Message>> WaitForMessages(IEnumerable<string> eventKeys)
        {
            var tcs = new TaskCompletionSource<IEnumerable<Message>>();
            int callbackCalled = 0;
            Action<IEnumerable<Message>> callback = null;

            callback = messages =>
            {
                if (Interlocked.Exchange(ref callbackCalled, 1) == 0)
                {
                    tcs.SetResult(messages);
                }

                foreach (var eventKey in eventKeys)
                {
                    LockedList<Action<IEnumerable<Message>>> callbacks;
                    if (_waitingTasks.TryGetValue(eventKey, out callbacks))
                    {
                        callbacks.Remove(callback);
                    }
                }
            };

            foreach (var eventKey in eventKeys)
            {
                var handlers = _waitingTasks.GetOrAdd(eventKey, _ => new LockedList<Action<IEnumerable<Message>>>());
                handlers.Add(callback);
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
                    var messages = entry.Value.Copy();
                    
                    foreach (var item in messages)
                    {
                        if (item.Expired)
                        {
                            entry.Value.Remove(item);
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