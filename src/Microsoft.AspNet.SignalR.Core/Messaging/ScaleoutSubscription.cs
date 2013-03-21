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
        private readonly IList<ScaleoutMappingStore> _stores;
        private readonly List<Cursor> _cursors;

        public ScaleoutSubscription(string identity,
                                    IList<string> eventKeys,
                                    string cursor,
                                    IList<ScaleoutMappingStore> stores,
                                    Func<MessageResult, object, Task<bool>> callback,
                                    int maxMessages,
                                    IPerformanceCounterManager counters,
                                    object state)
            : base(identity, eventKeys, callback, maxMessages, counters, state)
        {
            if (stores == null)
            {
                throw new ArgumentNullException("stores");
            }

            _stores = stores;

            List<Cursor> cursors = null;

            if (String.IsNullOrEmpty(cursor))
            {
                cursors = new List<Cursor>(stores.Count);
                for (int i = 0; i < stores.Count; i++)
                {
                    cursors.Add(new Cursor(i.ToString(CultureInfo.InvariantCulture), stores[i].MaxKey));
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
            var nextCursors = new ulong[_stores.Count];
            totalCount = 0;

            for (var i = 0; i < _stores.Count; ++i)
            {
                // Get the mapping for this stream
                ScaleoutMappingStore store = _stores[i];

                // See if we have a cursor for this key
                Cursor cursor = _cursors[i];
                nextCursors[i] = cursor.Id;

                bool consumed = true;

                // Try to find a local mapping for this payload
                IEnumerator<ScaleoutMapping> enumerator = store.GetEnumerator(cursor.Id);

                // If there's no node for this cursor id it's likely because we've
                // had an app domain restart and the cursor position is now invalid.
                if (enumerator == null)
                {
                    // Set it to the first id in this mapping
                    enumerator = store.GetMinEnumerator();

                    // Mark this cursor as unconsumed
                    consumed = false;
                }
                else if(consumed)
                {
                    // Skip this message since we consumed it already
                    enumerator.MoveNext();
                }

                // Stop if we got more than max messages
                while (totalCount < MaxMessages && enumerator.MoveNext())
                {
                    ScaleoutMapping mapping = enumerator.Current;

                    // It should be ok to lock here since groups aren't modified that often

                    // For each of the event keys we care about, extract all of the messages
                    // from the payload
                    for (var j = 0; j < EventKeys.Count; ++j)
                    {
                        string eventKey = EventKeys[j];

                        for (int k = 0; k < mapping.LocalKeyInfo.Count; k++)
                        {
                            var info = mapping.LocalKeyInfo[k];

                            if (info.Key.Equals(eventKey, StringComparison.OrdinalIgnoreCase))
                            {
                                MessageStoreResult<Message> storeResult = info.MessageStore.GetMessages(info.Id, 1);

                                if (storeResult.Messages.Count > 0)
                                {
                                    items.Add(storeResult.Messages);
                                    totalCount += storeResult.Messages.Count;
                                }
                            }
                        }
                    }

                    // Update the cursor id
                    nextCursors[i] = mapping.Id;
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
