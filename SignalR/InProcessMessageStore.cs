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
        private static List<InProcessMessage> _emptyMessageList = Enumerable.Empty<InProcessMessage>().ToList();

        private readonly ConcurrentDictionary<string, List<InProcessMessage>> _items = new ConcurrentDictionary<string, List<InProcessMessage>>(StringComparer.OrdinalIgnoreCase);
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
            var list = _items.GetOrAdd(key, _ => new List<InProcessMessage>());
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

        private IEnumerable<InProcessMessage> GetAllCoreSince(string key, long id)
        {
            var list = GetAllCore(key);
            int index;
            lock (list)
            {
                if (list.Count > 0 && list[0].MessageId > id)
                {
                    // All messages in the list are greater than the last message
                    return list.Select(m => m).ToList();
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
        }

        private List<InProcessMessage> GetAllCore(string key)
        {
            List<InProcessMessage> list;
            if (_items.TryGetValue(key, out list))
            {
                // Return a copy of the list
                return list;
            }
            return _emptyMessageList;
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
                    InProcessMessage[] messages;
                    lock (entry.Value)
                    {
                        messages = new InProcessMessage[entry.Value.Count];
                        entry.Value.CopyTo(messages);
                    }
                    foreach (var item in messages)
                    {
                        if (item.Expired)
                        {
                            lock (entry.Value)
                            {
                                entry.Value.Remove(item);
                            }
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