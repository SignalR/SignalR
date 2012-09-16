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

        public override string GetCursor()
        {
            return Cursor.MakeCursor(_cursors);
        }

        protected override void PerformWork(ref List<ArraySegment<Message>> items, out string nextCursor, ref int totalCount, out object state)
        {
            // The list of cursors represent (streamid, payloadid)
            var cursors = new List<Cursor>();

            foreach (var streamPair in _streamMappings)
            {
                // Get the mapping for this stream
                Linktionary<ulong, ScaleoutMapping> mapping = streamPair.Value;

                // See if we have a cursor for this key
                Cursor cursor = null;


                // REVIEW: We should optimize this
                int index = _cursors.FindIndex(c => c.Key == streamPair.Key);
                if (index != -1)
                {
                    cursor = _cursors[index];
                }
                else
                {
                    // Create a cursor and add it to the list.
                    // Point the Id to the first value
                    cursor = new Cursor
                    {
                        Id = GetCursorId(streamPair.Value),
                        Key = streamPair.Key
                    };
                }

                cursors.Add(cursor);

                // Try to find a local mapping for this payload
                LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>> node = mapping[cursor.Id];

                // Skip this node only if this isn't a new cursor
                if (node != null && index != -1)
                {
                    // Skip this node since we've already consumed it
                    node = node.Next;
                }

                while (node != null)
                {
                    KeyValuePair<ulong, ScaleoutMapping> pair = node.Value;

                    // Stop if we got more than max messages
                    if (totalCount >= MaxMessages)
                    {
                        break;
                    }

                    // For each of the event keys we care about, extract all of the messages
                    // from the payload
                    foreach (var eventKey in EventKeys)
                    {
                        LocalEventKeyInfo info;
                        if (pair.Value.EventKeyMappings.TryGetValue(eventKey, out info) && info.Count > 0)
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

            nextCursor = Cursor.MakeCursor(cursors);

            state = cursors;
        }

        protected override void BeforeInvoke(object state)
        {
            _cursors = (List<Cursor>)state;
        }

        private ulong GetCursorId(string key)
        {
            Linktionary<ulong, ScaleoutMapping> mapping;
            if (_streamMappings.TryGetValue(key, out mapping))
            {
                return GetCursorId(mapping);
            }

            return 0;
        }

        private ulong GetCursorId(Linktionary<ulong, ScaleoutMapping> mapping)
        {
            if (mapping.Last != null)
            {
                return mapping.Last.Value.Key;
            }

            return 0;
        }
    }
}
