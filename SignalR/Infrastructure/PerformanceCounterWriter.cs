using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Writes performance counter data to Windows performance counters.
    /// </summary>
    public class PerformanceCounterWriter : IPerformanceCounterWriter
    {
        private object _initLocker = new object();
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
                    var instanceName = hostContext.InstanceName() ?? Guid.NewGuid().ToString();
                    
                    LoadCounters(instanceName);

                    _hostShutdownToken = hostContext.HostShutdownToken();
                    if (_hostShutdownToken != null)
                    {
                        _hostShutdownToken.Register(() => UnloadCounters());
                    }

                    _countersInstalled = true;
                }
            }
        }

        public PerformanceCounter GetCounter(string counterName)
        {
            PerformanceCounter counter;
            _counters.TryGetValue(counterName, out counter);
            return counter;
        }

        private void LoadCounters(string instanceName)
        {
            foreach (var counterData in PerformanceCounters.Counters)
            {
                var counter = LoadCounter(PerformanceCounters.CategoryName, counterData.CounterName, instanceName);
                if (counter == null)
                {
                    // A counter is missing so we'll assume they're not installed properly, go into no-op mode
                    return;
                }
                _counters.Add(counter.CounterName, counter);

                // Initialize the sample
                counter.NextSample();
            }
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

        private static PerformanceCounter LoadCounter(string categoryName, string counterName, string instanceName)
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
