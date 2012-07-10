using System;
namespace SignalR.Infrastructure
{
    interface IPerformanceCounterWriter1
    {
        void Decrement(string counterName);
        void Increment(string counterName);
        void IncrementBy(string counterName, long value);
    }
}
