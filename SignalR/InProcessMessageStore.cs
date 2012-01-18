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
    public class InProcessMessageStore : IMessageStore
    {
        private static readonly Task<string> _zeroTask = TaskAsyncHelper.FromResult("0");
        private static List<InProcessMessage> _emptyMessageList = Enumerable.Empty<InProcessMessage>().ToList();

        private readonly ConcurrentDictionary<string, LockedList<InProcessMessage>> _items = new ConcurrentDictionary<string, LockedList<InProcessMessage>>(StringComparer.OrdinalIgnoreCase);
        // Interval to wait before cleaning up expired items
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);
        private readonly object _idLocker = new object();

        private long _lastMessageId = 0;
        private long _gcRunning = 0;

        private readonly Timer _timer;

        public InProcessMessageStore()
            : this(garbageCollectMessages: true)
        {
        }

        public InProcessMessageStore(bool garbageCollectMessages)
        {
            if (garbageCollectMessages)
            {
                _timer = new Timer(RemoveExpiredEntries, null, _cleanupInterval, _cleanupInterval);
            }
        }

        public long CurrentMessageCount()
        {
            return _items.Sum(kvp => kvp.Value.Count);
        }

        public Task Save(string key, object value)
        {
            var list = _items.GetOrAdd(key, _ => new LockedList<InProcessMessage>());

            lock (_idLocker)
            {
                // Only 1 save allowed at a time, to ensure messages are added to the list in order
                list.Add(new InProcessMessage(key, ++_lastMessageId, value));
            }
            
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called from scale-out message stores only
        /// </summary>
        protected internal Task Save(Message message)
        {
            return TaskAsyncHelper.Empty;
        }

        public Task<IEnumerable<Message>> GetAllSince(IEnumerable<string> keys, string id)
        {
            long longId;
            if (!Int64.TryParse(id, NumberStyles.Any, CultureInfo.InvariantCulture, out longId))
            {
                Debug.Fail("id must be a valid integer");
                throw new ArgumentException("id must be a valid integer", "id");
            }

            if (longId > _lastMessageId)
            {
                longId = 0;
            }

            var items = keys.SelectMany(k => GetAllCoreSince(k, longId))
                            .OrderBy(item => item.MessageId);

            return TaskAsyncHelper.FromResult<IEnumerable<Message>>(items);
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id)
        {
            if (id > _lastMessageId)
            {
                id = 0;
            }

            var items = GetAllCoreSince(key, id).OrderBy(item => item.Id);

            return TaskAsyncHelper.FromResult<IEnumerable<Message>>(items);
        }

        public Task<string> GetLastId()
        {
            if (_lastMessageId > 0)
            {
                return TaskAsyncHelper.FromResult(Interlocked.Read(ref _lastMessageId).ToString(CultureInfo.InvariantCulture));
            }

            return _zeroTask;
        }

        private IEnumerable<InProcessMessage> GetAllCoreSince(string key, long id)
        {
            var list = GetAllCore(key);
            int index;
            
            if (list.Count > 0 && list[0].MessageId > id)
            {
                // All messages in the list are greater than the last message
                return list;
            }

            index = list.FindLastIndex(msg => msg.MessageId <= id);
           
            if (index < 0)
            {
                return Enumerable.Empty<InProcessMessage>();
            }

            var startIndex = index + 1;

            if (startIndex >= list.Count)
            {
                return Enumerable.Empty<InProcessMessage>();
            }

            return list.GetRange(startIndex, list.Count - startIndex);
            
        }

        private List<InProcessMessage> GetAllCore(string key)
        {
            LockedList<InProcessMessage> list;
            if (_items.TryGetValue(key, out list))
            {
                // Return a copy of the list
                return list.Copy();
            }
            return _emptyMessageList;
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
                var entries = _items.ToList();

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

        private class InProcessMessage : Message
        {
            public long MessageId { get; set; }

            public InProcessMessage(string signalKey, long id, object value)
                : base(signalKey, id.ToString(CultureInfo.InvariantCulture), value)
            {
                MessageId = id;
            }
        }
    }
}