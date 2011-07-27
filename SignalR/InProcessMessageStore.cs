using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR {
    public class InProcessMessageStore : IMessageStore {
        private readonly ConcurrentDictionary<string, SafeSet<Message>> _items = new ConcurrentDictionary<string, SafeSet<Message>>(StringComparer.OrdinalIgnoreCase);
        // Interval to wait before cleaning up expired items
        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10);

        private static long _messageId = 0;
        private static object _idLocker = new object();

        private readonly Timer _timer;

        public InProcessMessageStore() {
            _timer = new Timer(RemoveExpiredEntries, null, _cleanupInterval, _cleanupInterval);
        }

        public Task Save(string key, object value) {
            return Save(new Message(key, Interlocked.Increment(ref _messageId), value));
        }

        protected internal Task Save(Message message) {
            var key = message.SignalKey;
            SafeSet<Message> list;
            if (!_items.TryGetValue(key, out list)) {
                list = new SafeSet<Message>();
                _items.TryAdd(key, list);
            }
            list.Add(message);
            if (message.Id > _messageId) {
                lock (_idLocker) {
                    if (message.Id > _messageId) {
                        _messageId = message.Id;
                    }
                }
            }
            
            return TaskAsyncHelper.Empty;
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id) {
            var items = GetAllCore(key).Where(item => item.Id > id)
                                       .OrderBy(item => item.Id);
            return TaskAsyncHelper.FromResult<IEnumerable<Message>>(items);

        }

        public Task<long?> GetLastId() {
            if (_messageId > 0) {
                return TaskAsyncHelper.FromResult<long?>(_messageId);
            }

            return TaskAsyncHelper.FromResult<long?>(null);
        }

        public Task<IEnumerable<Message>> GetAll(string key) {
            return TaskAsyncHelper.FromResult(GetAllCore(key));
        }

        private IEnumerable<Message> GetAllCore(string key) {
            SafeSet<Message> list;
            if (_items.TryGetValue(key, out list)) {
                // Return a copy of the list
                return list.GetSnapshot();
            }
            return Enumerable.Empty<Message>();
        }

        private void RemoveExpiredEntries(object state) {
            // Take a snapshot of the entries
            var entries = _items.ToList();

            // Remove all the expired ones
            foreach (var entry in entries) {
                foreach (var item in entry.Value.GetSnapshot()) {
                    if (item.Expired) {
                        entry.Value.Remove(item);
                    }
                }
            }
        }
    }
}