// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMappingStore
    {
        private StoreLink _minimum;
        private StoreLink _current;

        private int _size;

        private object _lockObject = new object();

        public ScaleoutMappingStore()
        {
            _minimum = new StoreLink();
            _current = _minimum;
        }

        public void Add(ulong id, IList<LocalEventKeyInfo> localKeyInfo)
        {
            if (id <= _current.MaxValue)
            {
                var store = new StoreLink();

                lock (_lockObject)
                {
                    // All existing readers lose messages right here (temporary)
                    _minimum = store;
                    _current.Next = store;
                    _current = store;
                }
            }

            var mapping = new ScaleoutMapping(id, localKeyInfo);

            // If the store is full then create a new one
            if (!_current.Add(mapping))
            {
                lock (_lockObject)
                {
                    var store = new StoreLink();
                    _current.Next = store;
                    _current = store;

                    if (_size >= 1000)
                    {
                        _minimum = _minimum.Next;
                    }
                    else
                    {
                        _size++;
                    }
                }
            }
        }

        public ulong MaxKey
        {
            get
            {
                lock (_lockObject)
                {
                    return _current.MaxValue ?? UInt64.MaxValue;
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This does some work")]
        public IEnumerator<ScaleoutMapping> GetMinEnumerator()
        {
            lock (_lockObject)
            {
                return new Enumerator(_minimum);
            }
        }

        public IEnumerator<ScaleoutMapping> GetEnumerator(ulong id)
        {
            // TODO: Optimize this lookup
            StoreLink store = FindStore(id);

            if (store == null)
            {
                return null;
            }

            return new Enumerator(store, id);
        }

        private StoreLink FindStore(ulong id)
        {
            lock (_lockObject)
            {
                StoreLink store = _minimum;

                while (store != null)
                {
                    if (store.HasValue(id))
                    {
                        return store;
                    }

                    store = store.Next;
                }

                return null;
            }
        }

        private struct Enumerator : IEnumerator<ScaleoutMapping>, IEnumerator
        {
            private StoreLink _current;

            private ArraySegment<ScaleoutMapping> _segment;
            private int _segmentOffset;
            private int _length;

            public Enumerator(StoreLink store)
                : this()
            {
                _current = store;
                _segment = store.GetSnapshot();
                _segmentOffset = -1;
                _length = _segment.Count;
            }

            public Enumerator(StoreLink store, ulong id)
                : this()
            {
                _current = store;
                _segment = store.GetSnapshot(id);
                _segmentOffset = _segment.Offset - 1;
                _length = _segment.Offset + _segment.Count;
            }

            public ScaleoutMapping Current
            {
                get
                {
                    return _segment.Array[_segmentOffset];
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
                if (_current == null)
                {
                    return false;
                }

                _segmentOffset++;

                if (_segmentOffset < _length)
                {
                    return true;
                }

                _current = _current.Next;

                if (_current != null)
                {
                    _segmentOffset = 0;
                    _segment = _current.GetSnapshot();
                    _length = _segment.Count;

                    return _segmentOffset < _length;
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
