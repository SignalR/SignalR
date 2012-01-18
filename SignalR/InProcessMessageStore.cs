using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;
using System.Globalization;

namespace SignalR
{
    public class InProcessMessageStore : IMessageStore
    {
        private static readonly Task<string> _zeroTask = TaskAsyncHelper.FromResult("0");

        private readonly ConcurrentDictionary<string, SafeSet<InProcessMessage>> _items = new ConcurrentDictionary<string, SafeSet<InProcessMessage>>(StringComparer.OrdinalIgnoreCase);
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

        public long CurrentMessageCount()
        {
            return _items.Sum(kvp => kvp.Value.Count);
        }

        public Task Save(string key, object value)
        {
            var list = _items.GetOrAdd(key, _ => new SafeSet<InProcessMessage>());
            lock (_idLocker)
            {
                var id = ++_lastMessageId;
                var message = new InProcessMessage(key, id, value);
                list.Add(message);
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

            var items = keys.SelectMany(k => GetAllCore(k).Where(item => item.MessageId > longId))
                            .OrderBy(item => item.Id);

            return TaskAsyncHelper.FromResult<IEnumerable<Message>>(items);
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id)
        {
            if (id > _lastMessageId)
            {
                id = 0;
            }

            var items = GetAllCore(key).Where(item => Int64.Parse(item.Id, CultureInfo.InvariantCulture) > id)
                                       .OrderBy(item => item.Id);

            return TaskAsyncHelper.FromResult<IEnumerable<Message>>(items);
        }

        public Task<string> GetLastId()
        {
            if (_lastMessageId > 0)
            {
                return TaskAsyncHelper.FromResult(_lastMessageId.ToString(CultureInfo.InvariantCulture));
            }

            return _zeroTask;
        }

        private IEnumerable<InProcessMessage> GetAllCore(string key)
        {
            SafeSet<InProcessMessage> list;
            if (_items.TryGetValue(key, out list))
            {
                // Return a copy of the list
                return list.GetSnapshot();
            }
            return Enumerable.Empty<InProcessMessage>();
        }

        private void RemoveExpiredEntries(object state)
        {
            try
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
            }
            catch (Exception ex)
            {
                // Exception on bg thread, bad! Log and swallow to stop the process exploding
                Trace.TraceError("Error during InProcessMessageStore clean up on background thread: {0}", ex);
            }
            finally
            {
                _gcRunning = false;
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