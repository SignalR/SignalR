using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
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
            _items = new SafeSet<T>(_addedItems);
        }

        public void Add(T item)
        {
            _items.Add(item);
            lock (_lock)
            {
                _addedItems.Add(item);
                _removedItems.Remove(item);
            }
        }

        public void Remove(T item)
        {
            _items.Remove(item);
            lock (_lock)
            {
                _addedItems.Remove(item);
                _removedItems.Add(item);
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
