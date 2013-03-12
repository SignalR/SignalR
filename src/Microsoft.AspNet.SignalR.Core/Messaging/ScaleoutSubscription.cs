// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutSubscription : Subscription
    {
        private readonly IList<IndexedDictionary> _streams;
        private readonly List<Cursor> _cursors;

        public ScaleoutSubscription(string identity,
                                    IList<string> eventKeys,
                                    string cursor,
                                    IList<IndexedDictionary> streams,
                                    Func<MessageResult, object, Task<bool>> callback,
                                    int maxMessages,
                                    IPerformanceCounterManager counters,
                                    object state)
            : base(identity, eventKeys, callback, maxMessages, counters, state)
        {
            if (streams == null)
            {
                throw new ArgumentNullException("streams");
            }

            _streams = streams;

            List<Cursor> cursors = null;

            if (String.IsNullOrEmpty(cursor))
            {
                cursors = new List<Cursor>(streams.Count);
                for (int i = 0; i < streams.Count; i++)
                {
                    cursors.Add(new Cursor(i.ToString(CultureInfo.InvariantCulture), streams[i].MaxKey));
                }
            }
            else
            {
                cursors = Cursor.GetCursors(cursor);
            }

            _cursors = cursors;
        }

        public override void WriteCursor(TextWriter textWriter)
        {
            Cursor.WriteCursors(textWriter, _cursors);
        }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "The list needs to be populated")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "It is called from the base class")]
        protected override void PerformWork(IList<ArraySegment<Message>> items, out int totalCount, out object state)
        {
            // The list of cursors represent (streamid, payloadid)
            var nextCursors = new ulong[_streams.Count];
            totalCount = 0;

            for (var i = 0; i < _streams.Count; ++i)
            {
                // Get the mapping for this stream
                IndexedDictionary stream = _streams[i];

                // See if we have a cursor for this key
                Cursor cursor = _cursors[i];
                nextCursors[i] = cursor.Id;

                bool consumed = true;

                // Try to find a local mapping for this payload
                LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>> node = stream.GetMapping(cursor.Id);

                // If there's no node for this cursor id it's likely because we've
                // had an app domain restart and the cursor position is now invalid.
                if (node == null)
                {
                    // Set it to the first id in this mapping                    
                    node = stream.GetMapping(stream.MinKey);

                    // Mark this cursor as unconsumed
                    consumed = false;
                }
                else if (consumed)
                {
                    // Skip this node since we've already consumed it
                    node = node.Next;
                }

                // Stop if we got more than max messages
                while (totalCount < MaxMessages && node != null)
                {
                    KeyValuePair<ulong, ScaleoutMapping> pair = node.Value;

                    // It should be ok to lock here since groups aren't modified that often

                    // For each of the event keys we care about, extract all of the messages
                    // from the payload
                    for (var j = 0; j < EventKeys.Count; ++j)
                    {
                        string eventKey = EventKeys[j];

                        LocalEventKeyInfo info;
                        if (pair.Value.EventKeyMappings.TryGetValue(eventKey, out info))
                        {
                            int maxMessages = Math.Min(info.Count, MaxMessages);
                            MessageStoreResult<Message> storeResult = info.Store.GetMessages(info.MinLocal, maxMessages);

                            if (storeResult.Messages.Count > 0)
                            {
                                items.Add(storeResult.Messages);
                                totalCount += storeResult.Messages.Count;
                            }
                        }
                    }

                    // Update the cursor id
                    nextCursors[i] = pair.Key;
                    node = node.Next;
                }
            }

            state = nextCursors;
        }

        protected override void BeforeInvoke(object state)
        {
            // Update the list of cursors before invoking anything
            var nextCursors = (ulong[])state;
            for (int i = 0; i < _cursors.Count; i++)
            {
                _cursors[i].Id = nextCursors[i];
            }
        }
    }
}
