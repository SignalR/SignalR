// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMappingStore
    {
        private readonly BoundedStoreManager _storeManager = new BoundedStoreManager();

        public void Add(ulong id, IList<LocalEventKeyInfo> localKeyInfo)
        {
            _storeManager.Add(new ScaleoutMapping(id, localKeyInfo));
        }

        public ulong MinKey
        {
            get
            {
                if (!_storeManager.Minimum.MinValue.HasValue)
                {
                    return 0;
                }
                return _storeManager.Minimum.MinValue.Value;
            }
        }

        public ulong MaxKey
        {
            get
            {
                if (!_storeManager.Current.MaxValue.HasValue)
                {
                    return 0;
                }
                return _storeManager.Current.MaxValue.Value;
            }
        }

        public ScaleoutMapping GetMapping(ulong id)
        {
            BoundedStore store = _storeManager.FindStore(id);

            if (store == null)
            {
                return null;
            }

            return store.FindMapping(id);
        }

        // TODO: Thread safety
        private class BoundedStoreManager
        {
            public BoundedStore Minimum { get; private set; }
            public BoundedStore Current { get; private set; }

            private ScaleoutMapping _previousMapping;

            public BoundedStoreManager()
            {
                Minimum = new BoundedStore();
                Current = Minimum;
            }

            public void Add(ScaleoutMapping mapping)
            {
                if (mapping.Id <= Current.MaxValue)
                {
                    var store = new BoundedStore();
                    Minimum = store;
                    Current = store;
                }

                if (!Current.Add(mapping))
                {
                    var store = new BoundedStore();
                    Current.Next = store;
                    Current = store;
                }

                if (_previousMapping != null)
                {
                    // Set the link
                    _previousMapping.Next = mapping;
                }

                // Keep track of the previous mapping
                _previousMapping = mapping;
            }

            public BoundedStore FindStore(ulong id)
            {
                BoundedStore store = Minimum;

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

        private class BoundedStore
        {
            private const int MaxBufferSize = 1000;

            private readonly ScaleoutMapping[] _data = new ScaleoutMapping[MaxBufferSize];
            private int _offset;

            public BoundedStore Next { get; set; }
            public ulong? MinValue { get; private set; }
            public ulong? MaxValue { get; private set; }

            public bool Add(ScaleoutMapping mapping)
            {
                MinValue = Math.Min(mapping.Id, MinValue ?? UInt64.MaxValue);
                MaxValue = Math.Max(mapping.Id, MaxValue ?? UInt64.MinValue);

                _data[_offset++] = mapping;

                if (_offset >= _data.Length)
                {
                    return false;
                }

                return true;
            }

            public bool HasValue(ulong id)
            {
                return id >= MinValue && id <= MaxValue;
            }

            public ScaleoutMapping FindMapping(ulong id)
            {
                int low = 0;
                int high = _offset;

                while (low <= high)
                {
                    int mid = (low + high) / 2;

                    ScaleoutMapping mapping = _data[mid];

                    if (id < mapping.Id)
                    {
                        high = mid - 1;
                    }
                    else if (id > mapping.Id)
                    {
                        low = mid + 1;
                    }
                    else if (id == mapping.Id)
                    {
                        return mapping;
                    }
                }

                return null;
            }
        }
    }
}
