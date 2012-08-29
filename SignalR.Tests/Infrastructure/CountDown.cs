using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SignalR.Tests
{
    public class CountDown
    {
        private int _count;
        private ManualResetEventSlim _wh = new ManualResetEventSlim(false);

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public CountDown(int count)
        {
            _count = count;
        }

        public void Dec()
        {
            if (Interlocked.Decrement(ref _count) == 0)
            {
                _wh.Set();
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            return _wh.Wait(timeout);
        }
    }
}
