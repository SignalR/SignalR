using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class ScaleoutSubscription : Subscription
    {
        private readonly ConcurrentDictionary<string, Linktionary<ulong, ScaleoutMapping>> _streamMappings;
        private List<Cursor> _cursors;

        private readonly object _lockObj = new object();

        public ScaleoutSubscription(string identity,
                                    IEnumerable<string> eventKeys,
                                    string cursor,
                                    ConcurrentDictionary<string, Linktionary<ulong, ScaleoutMapping>> streamMappings,
                                    Func<MessageResult, Task<bool>> callback,
                                    int maxMessages,
                                    IPerformanceCounterWriter counters)
            : base(identity, eventKeys, callback, maxMessages, counters)
        {
            _streamMappings = streamMappings;

            IEnumerable<Cursor> cursors = null;

            if (cursor == null)
            {
                cursors = from key in _streamMappings.Keys
                          select new Cursor
                          {
                              Key = key,
                              Id = GetCursorId(key)
                          };
            }
            else
            {
                cursors = Cursor.GetCursors(cursor);
            }

            _cursors = new List<Cursor>(cursors);
        }

        private ulong GetCursorId(string key)
        {
            Linktionary<ulong, ScaleoutMapping> mapping;
            if (_streamMappings.TryGetValue(key, out mapping) &&
                mapping.Last != null)
            {
                return mapping.Last.Value.Key;
            }

            return 0;
        }

        public override bool AddEvent(string key, Topic topic)
        {
            // This isn't relevant to us as EventKeys is up to date
            return false;
        }

        public override void RemoveEvent(string eventKey)
        {
            // This isn't relevant to us as EventKeys is up to date
        }

        public override void SetEventTopic(string key, Topic topic)
        {
            // This isn't relevant to us as EventKeys is up to date
        }

        public override string GetCursor()
        {
            return Cursor.MakeCursor(_cursors);
        }

        protected override void PerformWork(ref List<ArraySegment<Message>> items, out string nextCursor, ref int totalCount, out object state)
        {
            // The list of cursors represent (streamid, payloadid)
            var cursors = new List<Cursor>();

            for (int i = 0; i < _cursors.Count; i++)
            {
                Cursor cursor = Cursor.Clone(_cursors[i]);
                cursors.Add(cursor);

                Linktionary<ulong, ScaleoutMapping> mapping;
                if (_streamMappings.TryGetValue(cursor.Key, out mapping))
                {
                    // Try to find a local mapping for this payload
                    LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>> node = mapping[cursor.Id];

                    if(node != null)
                    {
                        // Skip this node since we've already consumed it
                        node = node.Next;
                    }

                    while (node != null)
                    {
                        KeyValuePair<ulong, ScaleoutMapping> pair = node.Value;

                        // For each of the event keys we care about
                        foreach (var eventKey in EventKeys)
                        {
                            LocalEventKeyInfo info;
                            if (pair.Value.EventKeyMappings.TryGetValue(eventKey, out info))
                            {
                                int maxMessages = Math.Min(info.Count, MaxMessages);
                                MessageStoreResult<Message> storeResult = info.Topic.Store.GetMessages(info.MinLocal, maxMessages);

                                if (storeResult.Messages.Count > 0)
                                {
                                    items.Add(storeResult.Messages);
                                    totalCount += storeResult.Messages.Count;
                                }
                            }
                        }

                        // Update the cursor id
                        cursor.Id = pair.Key;
                        node = node.Next;
                    }
                }
            }

            nextCursor = Cursor.MakeCursor(cursors);

            state = cursors;
        }

        protected override void BeforeInvoke(object state)
        {
            lock (_lockObj)
            {
                _cursors = (List<Cursor>)state;
            }
        }
    }
}
