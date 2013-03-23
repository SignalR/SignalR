// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMappingStore
    {
        private const int MaxMessages = 1000000;
        private ulong _maxKey = UInt64.MaxValue;

        private ScaleoutStore _store;

        public ScaleoutMappingStore()
        {
            _store = new ScaleoutStore(MaxMessages);
        }

        public void Add(ulong id, IList<LocalEventKeyInfo> localKeyInfo)
        {
            if (id < _maxKey)
            {
                // Do something here
            }

            _store.Add(new ScaleoutMapping(id, localKeyInfo));

            _maxKey = id;
        }

        public ulong MaxKey
        {
            get
            {
                return _maxKey;
            }
        }

        public IEnumerator<ScaleoutMapping> GetEnumerator(ulong id)
        {
            ArraySegment<ScaleoutMapping> segment = _store.GetMessages(id);

            return new ArraySegmentEnumerator<ScaleoutMapping>(segment);
        }

        private struct ArraySegmentEnumerator<T> : IEnumerator<T>, IEnumerator
        {
            private ArraySegment<T> _segment;
            private int _offset;
            private int _length;

            public ArraySegmentEnumerator(ArraySegment<T> segment)
                : this()
            {
                _segment = segment;
                _offset = _segment.Offset - 1;
                _length = _segment.Offset + _segment.Count;
            }

            public T Current
            {
                get
                {
                    return _segment.Array[_offset];
                }
            }

            public void Dispose()
            {

            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                _offset++;

                if (_offset < _length)
                {
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
