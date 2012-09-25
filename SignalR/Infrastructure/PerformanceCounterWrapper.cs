using System.Diagnostics;

namespace SignalR.Infrastructure
{
    internal class PerformanceCounterWrapper : IPerformanceCounter
    {
        private readonly PerformanceCounter _counter;

        public PerformanceCounterWrapper(PerformanceCounter counter)
        {
            _counter = counter;
        }

        public string CategoryName
        {
            get { return _counter.CategoryName; }
            set { _counter.CategoryName = value; }
        }

        public string CounterName
        {
            get { return _counter.CounterName; }
            set { _counter.CounterName = value; }
        }

        public string CounterHelp
        {
            get { return _counter.CounterHelp; }
        }

        public long RawValue
        {
            get { return _counter.RawValue; }
            set { _counter.RawValue = value; }
        }

        public long Decrement()
        {
            return _counter.Decrement();
        }

        public long Increment()
        {
            return _counter.Increment();
        }

        public long IncrementBy(long value)
        {
            return _counter.IncrementBy(value);
        }

        public void Close()
        {
            _counter.Close();
        }

        public void RemoveInstance()
        {
            _counter.RemoveInstance();
        }

        public void NextSample()
        {
            _counter.NextSample();
        }
    }
}
