// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    // TODO: This structure grows infinitely so we need to bound it
    public class IndexedDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _lookup = new ConcurrentDictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _list = new LinkedList<KeyValuePair<TKey, TValue>>();

        public bool TryAdd(TKey key, TValue value)
        {
            return _lookup.TryAdd(key, _list.AddLast(new KeyValuePair<TKey, TValue>(key, value)));
        }

        public TKey MinKey
        {
            get
            {
                return GetKey(_list.First);
            }
        }

        public TKey MaxKey
        {
            get
            {
                return GetKey(_list.Last);
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

        private static TKey GetKey(LinkedListNode<KeyValuePair<TKey, TValue>> node)
        {
            if (node == null)
            {
                return default(TKey);
            }
            return node.Value.Key;
        }
    }
}
