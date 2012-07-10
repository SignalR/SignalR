using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SignalR.Infrastructure
{
    public class PerformanceCounterInstaller
    {
        public void InstallCounters()
        {
            if (PerformanceCounterCategory.Exists(PerformanceCounters.CategoryName))
            {
                // Delete any existing counters
                PerformanceCounterCategory.Delete(PerformanceCounters.CategoryName);
            }

            var createDataCollection = new CounterCreationDataCollection(PerformanceCounters.Counters.ToArray());

            PerformanceCounterCategory.Create(PerformanceCounters.CategoryName,
                "SignalR application performance counters",
                PerformanceCounterCategoryType.MultiInstance,
                createDataCollection);
        }
    }
}
