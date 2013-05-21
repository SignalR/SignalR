using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Tests.Infrastructure
{
    public class CountDownRange<T>
    {
        private HashSet<T> _items;
        private HashSet<T> _seen;
        private ManualResetEventSlim _wh = new ManualResetEventSlim(false);

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
                        _wh.Set();
                    }

                    _seen.Add(item);

                    return true;
                }
            }

            return false;
        }

        public bool Wait(TimeSpan timeout)
        {
            return _wh.Wait(timeout);
        }
    }
}
