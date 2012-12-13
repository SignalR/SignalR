// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SignalRPerfCounterManager = Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager;

namespace Microsoft.AspNet.SignalR.Utils
{
    internal class PerformanceCounterInstaller
    {
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Called from non-static.")]
        public void UninstallCounters()
        {
            if (PerformanceCounterCategory.Exists(SignalRPerfCounterManager.CategoryName))
            {
                PerformanceCounterCategory.Delete(SignalRPerfCounterManager.CategoryName);
            }
        }
    }
}
