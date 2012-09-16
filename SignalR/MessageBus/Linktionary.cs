using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SignalR
{
    // TODO: This structure grows infinitely so we need to bound it
    public class Linktionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _lookup = new ConcurrentDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _list = new LinkedList<KeyValuePair<TKey, TValue>>();

        public void Add(TKey key, TValue value)
        {
            _lookup.TryAdd(key, _list.AddLast(new KeyValuePair<TKey, TValue>(key, value)));
        }

        public LinkedListNode<KeyValuePair<TKey, TValue>> Last
        {
            get
            {
                return _list.Last;
            }
        }

        public LinkedListNode<KeyValuePair<TKey, TValue>> this[TKey key]
        {
            get
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> value;
                if (_lookup.TryGetValue(key, out value))
                {
                    return value;
                }

                return null;
            }
        }
    }
}
