using System.Collections.Generic;
using System.Linq;

namespace SignalR.Infrastructure {
    internal class SafeSet<T> {
        private readonly HashSet<T> _items = new HashSet<T>();

        public SafeSet() {
        }

        public SafeSet(IEqualityComparer<T> comparer) {
            _items = new HashSet<T>(comparer);
        }

        public SafeSet(IEnumerable<T> items) {
            _items = new HashSet<T>(items);
        }

        public IEnumerable<T> GetSnapshot() {
            lock (_items) {
                return _items.ToArray();
            }
        }

        public void Add(T item) {
            lock (_items) {
                _items.Add(item);
            }
        }

        public void Remove(T item) {
            lock (_items) {
                _items.Remove(item);
            }
        }
    }
}
