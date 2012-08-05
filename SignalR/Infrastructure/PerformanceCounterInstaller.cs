using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Manages installation of performance counters for SignalR applications.
    /// </summary>
    public class PerformanceCounterInstaller
    {
        /// <summary>
        /// Installs SignalR performance counters.
        /// </summary>
        public IList<string> InstallCounters()
        {
            // Delete any existing counters
            UninstallCounters();

            var createDataCollection = new CounterCreationDataCollection(PerformanceCounters.Counters);
            
            PerformanceCounterCategory.Create(PerformanceCounters.CategoryName,
                "SignalR application performance counters",
                PerformanceCounterCategoryType.MultiInstance,
                createDataCollection);

            return PerformanceCounters.Counters.Select(c => c.CounterName).ToList();
        }

        /// <summary>
        /// Uninstalls SignalR performance counters.
        /// </summary>
        public void UninstallCounters()
        {
            if (PerformanceCounterCategory.Exists(PerformanceCounters.CategoryName))
            {
                PerformanceCounterCategory.Delete(PerformanceCounters.CategoryName);
            }
        }
    }
}
