using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Tests.Infrastructure
{
    internal class OrderedCountDownRange<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private T _value;
        private readonly ManualResetEventSlim _wh = new ManualResetEventSlim(false);

        public OrderedCountDownRange(IEnumerable<T> range)
        {
            _enumerator = range.GetEnumerator();
            _enumerator.MoveNext();
            _value = _enumerator.Current;
        }

        public bool Expect(T item)
        {
            bool result = Object.Equals(_value, item);

            if (result)
            {
                if (_enumerator.MoveNext())
                {
                    _value = _enumerator.Current;
                }
                else
                {
                    _wh.Set();
                }
            }

            return result;
        }

        public bool Wait(TimeSpan timeout)
        {
            return _wh.Wait(timeout);
        }
    }
}
