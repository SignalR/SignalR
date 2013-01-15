// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class DiffSet<T>
    {
        private readonly SafeSet<T> _items;
        private readonly HashSet<T> _addedItems;
        private readonly HashSet<T> _removedItems;
        private readonly object _lock = new object();

        public DiffSet(IEnumerable<T> items)
        {
            _addedItems = new HashSet<T>(items);
            _removedItems = new HashSet<T>();
            // We don't want to re-enumerate items
            _items = new SafeSet<T>(_addedItems);
        }

        public bool Add(T item)
        {
            lock (_lock)
            {
                if (_items.Add(item))
                {
                    if (!_removedItems.Remove(item))
                    {
                        _addedItems.Add(item);
                    }
                    return true;
                }
                return false;
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                if (_items.Remove(item))
                {
                    if (!_addedItems.Remove(item))
                    {
                        _removedItems.Add(item);
                    }
                    return true;
                }
                return false;
            }
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public ICollection<T> GetSnapshot()
        {
            return _items.GetSnapshot();
        }

        public DiffPair<T> GetDiff()
        {
            lock (_lock)
            {
                var pair = new DiffPair<T>
                {
                    Added = new List<T>(_addedItems),
                    Removed = new List<T>(_removedItems)
                };
                _addedItems.Clear();
                _removedItems.Clear();
                return pair;
            }
        }
}
}
