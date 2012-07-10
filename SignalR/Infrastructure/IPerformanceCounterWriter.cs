using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalR.Infrastructure
{
    public interface IPerformanceCounterWriter
    {
        void Decrement(string counterName);
        void Increment(string counterName);
        void IncrementBy(string counterName, long value);
    }
}
