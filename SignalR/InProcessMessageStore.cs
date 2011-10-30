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
    public class InProcessMessageStore : IMessageStore
    {
        private readonly ConcurrentDictionary<string, SafeSet<Message>> _items = new ConcurrentDictionary<string, SafeSet<Message>>(StringComparer.OrdinalIgnoreCase);
        // Interval to wait before cleaning up expired items
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        private long _lastMessageId = 0;
        private readonly object _idLocker = new object();
        private bool _gcRunning;

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

        public Task Save(string key, object value)
        {
            var message = new Message(key, Interlocked.Increment(ref _lastMessageId), value);
            return Save(message);
        }

        protected internal Task Save(Message message)
        {
            var key = message.SignalKey;

            var list = _items.GetOrAdd(key, _ => new SafeSet<Message>());
            list.Add(message);

            if (message.Id > _lastMessageId)
            {
                lock (_idLocker)
                {
                    if (message.Id > _lastMessageId)
                    {
                        _lastMessageId = message.Id;
                    }
                }
            }

            return TaskAsyncHelper.Empty;
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id)
        {
            if (id > _lastMessageId)
            {
                id = 0;
            }

            var items = GetAllCore(key).Where(item => item.Id > id)
                                       .OrderBy(item => item.Id);

            return TaskAsyncHelper.FromResult<IEnumerable<Message>>(items);
        }

        public Task<long?> GetLastId()
        {
            if (_lastMessageId > 0)
            {
                return TaskAsyncHelper.FromResult<long?>(_lastMessageId);
            }

            return TaskAsyncHelper.FromResult<long?>(null);
        }

        private IEnumerable<Message> GetAllCore(string key)
        {
            SafeSet<Message> list;
            if (_items.TryGetValue(key, out list))
            {
                // Return a copy of the list
                return list.GetSnapshot();
            }
            return Enumerable.Empty<Message>();
        }

        private void RemoveExpiredEntries(object state)
        {
            if (_gcRunning || Debugger.IsAttached)
            {
                return;
            }

            _gcRunning = true;

            // Take a snapshot of the entries
            var entries = _items.ToList();

            // Remove all the expired ones
            foreach (var entry in entries)
            {
                foreach (var item in entry.Value.GetSnapshot())
                {
                    if (item.Expired)
                    {
                        entry.Value.Remove(item);
                    }
                }
            }

            _gcRunning = false;
        }
    }
}