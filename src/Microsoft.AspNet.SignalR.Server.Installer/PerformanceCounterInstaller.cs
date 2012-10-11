using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.SignalR.Infrastructure;
using SignalRPerfCounterManager = Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager;

namespace Microsoft.AspNet.SignalR
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

            var counterCreationData = SignalRPerfCounterManager.GetCounterPropertyInfo()
                .Select(p =>
                    {
                        var attribute = SignalRPerfCounterManager.GetPerformanceCounterAttribute(p);
                        return new CounterCreationData(attribute.Name, attribute.Description, attribute.CounterType);
                    })
                .ToArray();

            var createDataCollection = new CounterCreationDataCollection(counterCreationData);

            PerformanceCounterCategory.Create(SignalRPerfCounterManager.CategoryName,
                "SignalR application performance counters",
                PerformanceCounterCategoryType.MultiInstance,
                createDataCollection);

            return counterCreationData.Select(c => c.CounterName).ToList();
        }

        /// <summary>
        /// Uninstalls SignalR performance counters.
        /// </summary>
        public void UninstallCounters()
        {
            if (PerformanceCounterCategory.Exists(SignalRPerfCounterManager.CategoryName))
            {
                PerformanceCounterCategory.Delete(SignalRPerfCounterManager.CategoryName);
            }
        }
    }
}
