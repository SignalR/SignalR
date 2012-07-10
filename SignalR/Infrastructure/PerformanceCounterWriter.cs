using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SignalR.Infrastructure
{
    public class PerformanceCounterWriter : IPerformanceCounterWriter
    {
        private const string InstanceNameKey = "SignalR_AppInstanceName";
        private const string Category = "SignalR";
        private readonly string _instanceName;

        private readonly ConcurrentDictionary<string, PerformanceCounter> _counters = new ConcurrentDictionary<string, PerformanceCounter>();

        public PerformanceCounterWriter()
            : this((AppDomain.CurrentDomain.GetData(InstanceNameKey) ?? Guid.NewGuid()).ToString())
        {

        }

        public PerformanceCounterWriter(string instanceName)
        {    
            _instanceName = instanceName;   
        }

        public void Increment(string counterName)
        {
            if (String.IsNullOrEmpty(counterName))
            {
                throw new ArgumentException("A value for parameter 'counterName' is required.", "counterName");
            }

            var counter = GetOrAddCounter(counterName);
            counter.Increment();
        }

        public void IncrementBy(string counterName, long value)
        {
            if (String.IsNullOrEmpty(counterName))
            {
                throw new ArgumentException("A value for parameter 'counterName' is required.", "counterName");
            }

            var counter = GetOrAddCounter(counterName);
            counter.IncrementBy(value);
        }

        public void Decrement(string counterName)
        {
            if (String.IsNullOrEmpty(counterName))
            {
                throw new ArgumentException("A value for parameter 'counterName' is required.", "counterName");
            }

            var counter = GetOrAddCounter(counterName);
            counter.Decrement();
        }

        private PerformanceCounter GetOrAddCounter(string counterName)
        {
            return _counters.GetOrAdd(counterName, cn =>
            {
                try
                {
                    if (PerformanceCounterCategory.CounterExists(counterName, Category))
                    {
                        return new PerformanceCounter(Category, cn, _instanceName, readOnly: false);
                    }
                    return null;
                }
                catch (InvalidOperationException) { return null; }
                catch (UnauthorizedAccessException) { return null; }
            });
        }
    }
}
