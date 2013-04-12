// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    // TODO: This structure grows infinitely so we need to bound it
    public class IndexedDictionary
    {
        private readonly ConcurrentDictionary<ulong, LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>>> _lookup = new ConcurrentDictionary<ulong, LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>>>();
        private readonly LinkedList<KeyValuePair<ulong, ScaleoutMapping>> _list = new LinkedList<KeyValuePair<ulong, ScaleoutMapping>>();

        public bool TryAdd(ulong key, ScaleoutMapping value)
        {
            return _lookup.TryAdd(key, _list.AddLast(new KeyValuePair<ulong, ScaleoutMapping>(key, value)));
        }

        public ulong MinKey
        {
            get
            {
                return GetKey(_list.First);
            }
        }

        public ulong MaxKey
        {
            get
            {
                return GetKey(_list.Last);
            }
        }

        public LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>> GetMapping(ulong key)
        {
            LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>> value;
            if (_lookup.TryGetValue(key, out value))
            {
                return value;
            }

            return null;
        }

        private static ulong GetKey(LinkedListNode<KeyValuePair<ulong, ScaleoutMapping>> node)
        {
            if (node == null)
            {
                return default(ulong);
            }
            return node.Value.Key;
        }

        public void Clear()
        {
            _list.Clear();
            _lookup.Clear();
        }
    }
}
