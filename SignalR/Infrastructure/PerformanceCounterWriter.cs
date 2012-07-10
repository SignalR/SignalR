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
        private readonly string _instanceName;
        private bool _countersInstalled = false;

        private readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>();

        public PerformanceCounterWriter()
            : this((AppDomain.CurrentDomain.GetData(InstanceNameKey) ?? Guid.NewGuid()).ToString())
        {

        }

        public PerformanceCounterWriter(string instanceName)
        {    
            _instanceName = instanceName;
            LoadCounters();
        }

        private void LoadCounters()
        {
            foreach (var counterData in PerformanceCounters.Counters)
            {
                var counter = GetCounter(PerformanceCounters.CategoryName, counterData.CounterName, _instanceName);
                if (counter == null)
                {
                    // A counter is missing, go into no-op mode
                    // REVIEW: Should we trace a message here?
                    return;
                }
                _counters.Add(counter.CounterName, counter);
                
                // Initialize the sample
                counter.NextSample();
            }

            _countersInstalled = true;
        }

        public void Increment(string counterName)
        {
            if (String.IsNullOrEmpty(counterName))
            {
                throw new ArgumentException("A value for parameter 'counterName' is required.", "counterName");
            }

            if (!_countersInstalled)
            {
                return;
            }

            PerformanceCounter counter;
            if (_counters.TryGetValue(counterName, out counter))
            {
                counter.Increment();
            }
        }

        public void IncrementBy(string counterName, long value)
        {
            if (String.IsNullOrEmpty(counterName))
            {
                throw new ArgumentException("A value for parameter 'counterName' is required.", "counterName");
            }

            if (!_countersInstalled)
            {
                return;
            }

            PerformanceCounter counter;
            if (_counters.TryGetValue(counterName, out counter))
            {
                counter.IncrementBy(value);
            }
        }

        public void Decrement(string counterName)
        {
            if (String.IsNullOrEmpty(counterName))
            {
                throw new ArgumentException("A value for parameter 'counterName' is required.", "counterName");
            }

            if (!_countersInstalled)
            {
                return;
            }

            PerformanceCounter counter;
            if (_counters.TryGetValue(counterName, out counter))
            {
                counter.Decrement();
            }
        }

        private static PerformanceCounter GetCounter(string categoryName, string counterName, string instanceName)
        {
            try
            {
                if (PerformanceCounterCategory.CounterExists(counterName, PerformanceCounters.CategoryName))
                {
                    return new PerformanceCounter(categoryName, counterName, instanceName, readOnly: false);
                }
                return null;
            }
            catch (InvalidOperationException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
    }
}
