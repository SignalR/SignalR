// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutSubscription : Subscription
    {
        private const string _scaleoutCursorPrefix = "s-";

        private readonly IList<ScaleoutMappingStore> _streams;
        private readonly List<Cursor> _cursors;
        private readonly TraceSource _trace;
        private readonly string _tstatEventKey;

        public ScaleoutSubscription(string identity,
                                    IList<string> eventKeys,
                                    string cursor,
                                    IList<ScaleoutMappingStore> streams,
                                    Func<MessageResult, object, Task<bool>> callback,
                                    int maxMessages,
                                    ITraceManager traceManager,
                                    IPerformanceCounterManager counters,
                                    object state)
            : base(identity, eventKeys, callback, maxMessages, counters, state)
        {
            if (streams == null)
            {
                throw new ArgumentNullException("streams");
            }

            if (traceManager == null)
            {
                throw new ArgumentNullException(nameof(traceManager));
            }

            _streams = streams;

            List<Cursor> cursors = null;

            if (String.IsNullOrEmpty(cursor))
            {
                cursors = new List<Cursor>();
            }
            else
            {
                cursors = Cursor.GetCursors(cursor, _scaleoutCursorPrefix);

                // If the cursor had a default prefix, "d-", cursors might be null
                if (cursors == null)
                {
                    cursors = new List<Cursor>();
                }
                // If the streams don't match the cursors then throw it out
                else if (cursors.Count != _streams.Count)
                {
                    cursors.Clear();
                }
            }

            // No cursors so we need to populate them from the list of streams
            if (cursors.Count == 0)
            {
                for (int streamIndex = 0; streamIndex < _streams.Count; streamIndex++)
                {
                    AddCursorForStream(streamIndex, cursors);
                }
            }

            _cursors = cursors;
            _trace = traceManager["SignalR." + typeof(ScaleoutSubscription).Name];

            // Check if this subscription is for the event key we care about
            _tstatEventKey = eventKeys.FirstOrDefault(s => s.Equals("hu-SentinelCloudHub.TSTATK12343"));

            if (_tstatEventKey != null)
            {
                _trace.TraceInformation($"TSTAT Subscription initialized: {_tstatEventKey}");
            }
        }

        public override void WriteCursor(TextWriter textWriter)
        {
            Cursor.WriteCursors(textWriter, _cursors, _scaleoutCursorPrefix);
        }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "The list needs to be populated")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "It is called from the base class")]
        protected override void PerformWork(IList<ArraySegment<Message>> items, out int totalCount, out object state)
        {
            if (_tstatEventKey != null)
            {
                PerformWorkForTStat();
            }

            // The list of cursors represent (streamid, payloadid)
            var nextCursors = new ulong?[_cursors.Count];
            totalCount = 0;

            // Get the enumerator so that we can extract messages for this subscription
            IEnumerator<Tuple<ScaleoutMapping, int>> enumerator = GetMappings().GetEnumerator();

            ulong? startMapping = null;
            ScaleoutMapping mapping = null;
            while (totalCount < MaxMessages && enumerator.MoveNext())
            {
                mapping = enumerator.Current.Item1;
                int streamIndex = enumerator.Current.Item2;

                if (startMapping == null)
                {
                    startMapping = mapping.Id;
                }

                ulong? nextCursor = nextCursors[streamIndex];

                // Only keep going with this stream if the cursor we're looking at is bigger than
                // anything we already processed
                if (nextCursor == null || mapping.Id > nextCursor)
                {
                    var oldCount = totalCount;
                    ulong mappingId = ExtractMessages(streamIndex, mapping, items, ref totalCount);

                    if (_tstatEventKey != null)
                    {
                        var extracted = totalCount - oldCount;
                        if (extracted > 0)
                        {
                            _trace.TraceInformation($"Extracted {extracted} messages from mapping {mapping.Id}. Next cursor: {mappingId}.");
                        }
                    }

                    // Update the cursor id
                    nextCursors[streamIndex] = mappingId;
                }
                else
                {
                    _trace.TraceInformation($"Mapping ID {mapping.Id} is behind cursor {nextCursor.Value}.");
                }
            }

            if (_tstatEventKey != null)
            {
                _trace.TraceInformation($"Extracted {totalCount} messages from mappings {startMapping?.ToString() ?? "<null>"} to {mapping?.Id.ToString() ?? "<null>"}");
            }

            state = nextCursors;
        }

        // Used for WinDBG breakpoint hax.
        [System.Runtime.CompilerServices.MethodImpl(MethodImplOptions.NoInlining)]
        private void PerformWorkForTStat()
        {
            _trace.TraceInformation($"Processing TSTAT: {_tstatEventKey}");
        }

        protected override void BeforeInvoke(object state)
        {
            // Update the list of cursors before invoking anything
            var nextCursors = (ulong?[])state;
            for (int i = 0; i < _cursors.Count; i++)
            {
                // Only update non-null entries
                ulong? nextCursor = nextCursors[i];

                if (nextCursor.HasValue)
                {
                    Cursor cursor = _cursors[i];

                    _trace.TraceVerbose("Setting cursor {0} value {1} to {2} [{3}]",
                        i, cursor.Id, nextCursor.Value, Identity);

                    cursor.Id = nextCursor.Value;
                }
            }
        }

        private IEnumerable<Tuple<ScaleoutMapping, int>> GetMappings()
        {
            var enumerators = new List<CachedStreamEnumerator>();

            var singleStream = _streams.Count == 1;

            for (var streamIndex = 0; streamIndex < _streams.Count; ++streamIndex)
            {
                // Get the mapping for this stream
                ScaleoutMappingStore store = _streams[streamIndex];

                Cursor cursor = _cursors[streamIndex];

                if (_tstatEventKey != null)
                {
                    _trace.TraceInformation($"TSTAT {_tstatEventKey} Subscription cursor value: {cursor.Id} {cursor.Key}");
                }

                // Try to find a local mapping for this payload
                var enumerator = new CachedStreamEnumerator(store.GetEnumerator(cursor.Id),
                                                            streamIndex);

                enumerators.Add(enumerator);
            }

            var counter = 0;
            ulong? lastMapping = null;
            while (enumerators.Count > 0)
            {
                ScaleoutMapping minMapping = null;
                CachedStreamEnumerator minEnumerator = null;

                for (int i = enumerators.Count - 1; i >= 0; i--)
                {
                    counter += 1;

                    CachedStreamEnumerator enumerator = enumerators[i];

                    ScaleoutMapping mapping;
                    if (enumerator.TryMoveNext(out mapping))
                    {
                        if (minMapping == null || mapping.ServerCreationTime < minMapping.ServerCreationTime)
                        {
                            minMapping = mapping;
                            minEnumerator = enumerator;
                        }
                    }
                    else
                    {
                        enumerators.RemoveAt(i);
                    }
                }

                if (minMapping != null)
                {
                    minEnumerator.ClearCachedValue();

                    // This log is only really viable for Redis, where there's only a single stream.
                    if (singleStream && lastMapping.HasValue && minMapping.Id < lastMapping.Value)
                    {
                        _trace.TraceEvent(TraceEventType.Error, 0, $"Mapping regression occurred (connection ID: {Identity}). The next mapping {minMapping.Id} was less than the previous mapping {lastMapping.Value}");
                    }
                    lastMapping = minMapping.Id;

                    yield return Tuple.Create(minMapping, minEnumerator.StreamIndex);
                }
            }
        }

        private ulong ExtractMessages(int streamIndex, ScaleoutMapping mapping, IList<ArraySegment<Message>> items, ref int totalCount)
        {
            // For each of the event keys we care about, extract all of the messages
            // from the payload
            lock (EventKeys)
            {
                for (var i = 0; i < EventKeys.Count; ++i)
                {
                    string eventKey = EventKeys[i];

                    for (int j = 0; j < mapping.LocalKeyInfo.Count; j++)
                    {
                        LocalEventKeyInfo info = mapping.LocalKeyInfo[j];

                        var isTstatStore = string.Equals(info.Key, _tstatEventKey);

                        // Capture info.MessageStore because it could be GC'd while we're working with it.
                        var messageStore = info.MessageStore;
                        if (messageStore != null && info.Key.Equals(eventKey, StringComparison.OrdinalIgnoreCase))
                        {
                            if (isTstatStore)
                            {
                                _trace.TraceInformation($"Processing MessageStore for {info.Key}. Count: {messageStore.GetMessageCount()}.");
                            }
                            MessageStoreResult<Message> storeResult = messageStore.GetMessages(info.Id, 1);

                            if (storeResult.Messages.Count > 0)
                            {
                                // TODO: Figure out what to do when we have multiple event keys per mapping
                                Message message = storeResult.Messages.Array[storeResult.Messages.Offset];

                                // Only add the message to the list if the stream index matches
                                if (message.StreamIndex == streamIndex)
                                {
                                    items.Add(storeResult.Messages);
                                    totalCount += storeResult.Messages.Count;

                                    _trace.TraceVerbose("Adding {0} message(s) for mapping id: {1}, event key: '{2}', event id: {3}, streamIndex: {4}",
                                        storeResult.Messages.Count, mapping.Id, info.Key, info.Id, streamIndex);

                                    // We got a mapping id bigger than what we expected which
                                    // means we missed messages. Use the new mappingId.
                                    if (message.MappingId > mapping.Id)
                                    {
                                        _trace.TraceEvent(TraceEventType.Verbose, 0, $"Extracted additional messages, updating cursor to new Mapping ID: {message.MappingId}");
                                        return message.MappingId;
                                    }
                                }
                                else
                                {
                                    // REVIEW: When the stream indexes don't match should we leave the mapping id as is?
                                    // If we do nothing then we'll end up querying old cursor ids until
                                    // we eventually find a message id that matches this stream index.

                                    _trace.TraceInformation(
                                        "Stream index mismatch. Mapping id: {0}, event key: '{1}', event id: {2}, message.StreamIndex: {3}, streamIndex: {4}",
                                            mapping.Id, info.Key, info.Id, message.StreamIndex, streamIndex);
                                }
                            }
                        }
                        else if(messageStore == null && isTstatStore)
                        {
                            _trace.TraceInformation($"MessageStore {info.Key} was garbage collected!");
                        }
                    }
                }
            }

            return mapping.Id;
        }

        private void AddCursorForStream(int streamIndex, List<Cursor> cursors)
        {
            ScaleoutMapping maxMapping = _streams[streamIndex].MaxMapping;

            ulong id = UInt64.MaxValue;
            string key = streamIndex.ToString(CultureInfo.InvariantCulture);

            if (maxMapping != null)
            {
                id = maxMapping.Id;
            }

            var newCursor = new Cursor(key, id);
            cursors.Add(newCursor);
        }

        private class CachedStreamEnumerator
        {
            private readonly IEnumerator<ScaleoutMapping> _enumerator;
            private ScaleoutMapping _cachedValue;

            public CachedStreamEnumerator(IEnumerator<ScaleoutMapping> enumerator, int streamIndex)
            {
                _enumerator = enumerator;
                StreamIndex = streamIndex;
            }

            public int StreamIndex { get; private set; }

            public bool TryMoveNext(out ScaleoutMapping mapping)
            {
                mapping = null;

                if (_cachedValue != null)
                {
                    mapping = _cachedValue;
                    return true;
                }

                if (_enumerator.MoveNext())
                {
                    mapping = _enumerator.Current;
                    _cachedValue = mapping;
                    return true;
                }

                return false;
            }

            public void ClearCachedValue()
            {
                _cachedValue = null;
            }
        }
    }
}
