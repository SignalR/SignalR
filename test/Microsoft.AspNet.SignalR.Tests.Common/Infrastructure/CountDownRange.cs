// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Infrastructure
{
    public class CountDownRange<T>
    {
        private HashSet<T> _items;
        private HashSet<T> _seen;
        private TaskCompletionSource<object> _wh = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public CountDownRange(IEnumerable<T> range)
        {
            _items = new HashSet<T>(range);
            _seen = new HashSet<T>();
        }

        public int Count
        {
            get
            {
                lock (_items)
                {
                    return _items.Count;
                }
            }
        }

        public IEnumerable<T> Seen
        {
            get
            {
                lock (_seen)
                {
                    return _seen.ToList();
                }
            }
        }

        public IEnumerable<T> Left
        {
            get
            {
                lock (_items)
                {
                    return _items.ToList();
                }
            }
        }

        public bool Mark(T item)
        {
            lock (_items)
            {
                if (_items.Remove(item))
                {
                    if (_items.Count == 0)
                    {
                        _wh.TrySetResult(null);
                    }

                    _seen.Add(item);

                    return true;
                }
            }

            return false;
        }

        public Task WaitAsync() => _wh.Task;
    }
}
