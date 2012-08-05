using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SignalR.Infrastructure
{
    public class PerformanceCounterWriter : IPerformanceCounterWriter
    {
        private object _initLocker = new object();
        private string _instanceName;
        private bool _countersInstalled = false;
        private CancellationToken _hostShutdownToken;

        private readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>();

        public void Initialize(HostContext hostContext)
        {
            if (_countersInstalled)
            {
                return;
            }

            lock (_initLocker)
            {
                if (!_countersInstalled)
                {
                    _instanceName = hostContext.InstanceName() ?? Guid.NewGuid().ToString();
                    _hostShutdownToken = hostContext.HostShutdownToken();
                    if (_hostShutdownToken != null)
                    {
                        LoadCounters();
                        _hostShutdownToken.Register(() => UnloadCounters());
                    }
                }
            }
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

        private void UnloadCounters()
        {
            if (!_countersInstalled)
            {
                return;
            }

            foreach (var counter in _counters.Values)
            {
                counter.RemoveInstance();
            }
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
