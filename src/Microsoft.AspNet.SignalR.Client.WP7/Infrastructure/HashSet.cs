using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal class HashSet<T> : ICollection<T>
    {
        private readonly Dictionary<T, object> _set = new Dictionary<T, object>();

        public int Count
        {
            get { return _set.Count; }
        }

        public bool IsReadOnly
        {
            get { return false;  }
        }

        public void Add(T item)
        {
            _set[item] = null;
        }

        public void Clear()
        {
            _set.Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)_set.Keys).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _set.Keys.CopyTo(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set.Keys.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _set.Keys.GetEnumerator();
        }

        public bool Remove(T item)
        {
            return _set.Remove(item);
        }
    }
}
