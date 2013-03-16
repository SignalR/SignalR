// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMappingStore
    {
        private ScaleoutMapping _previousMapping;
        private StoreLink _minimum;
        private StoreLink _current;

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
                _current.Next = store;
                _current = store;
            }

            var mapping = new ScaleoutMapping(id, localKeyInfo, _current);

            // If the store is full then create a new one
            if (!_current.Add(mapping))
            {
                var store = new StoreLink();
                _current.Next = store;
                _current = store;
            }

            if (_previousMapping != null)
            {
                // Set the link
                _previousMapping.Next = mapping;
            }

            // Keep track of the previous mapping
            _previousMapping = mapping;
        }

        public ulong MinKey
        {
            get
            {
                if (!_minimum.MinValue.HasValue)
                {
                    return UInt64.MinValue;
                }
                return _minimum.MinValue.Value;
            }
        }

        public ulong MaxKey
        {
            get
            {
                if (!_current.MaxValue.HasValue)
                {
                    return UInt64.MaxValue;
                }
                return _current.MaxValue.Value;
            }
        }

        public IScaleoutMapping GetMapping(ulong id)
        {
            StoreLink store = FindStore(id);

            if (store == null)
            {
                return null;
            }

            return store.FindMapping(id);
        }

        private StoreLink FindStore(ulong id)
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
}
