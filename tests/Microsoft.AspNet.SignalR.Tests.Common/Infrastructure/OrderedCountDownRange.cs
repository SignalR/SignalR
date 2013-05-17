using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Tests.Infrastructure
{
    public class OrderedCountDownRange<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private T _value;
        private readonly ManualResetEventSlim _wh = new ManualResetEventSlim();
        private bool _result;

        public OrderedCountDownRange(IEnumerable<T> range)
        {
            _enumerator = range.GetEnumerator();
            _enumerator.MoveNext();
            _value = _enumerator.Current;
            _result = true;
        }

        public bool Expect(T item)
        {
            _result = Object.Equals(_value, item) && _result;

            if (_result)
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

            return _result;
        }

        public bool Wait(TimeSpan timeout)
        {
            return _wh.Wait(timeout);
        }
    }
}
