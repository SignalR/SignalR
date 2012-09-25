using System;
using System.Diagnostics;
using System.Globalization;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public static class HubManagerExtensions
    {
        public static HubDescriptor EnsureHub(this IHubManager hubManager, string hubName, params IPerformanceCounter[] counters)
        {
            var descriptor = hubManager.GetHub(hubName);

            if (descriptor == null)
            {
                for (var i = 0; i < counters.Length; i++)
                {
                    counters[i].Increment();
                }
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' hub could not be resolved.", hubName));
            }

            return descriptor;
        }
    }
}
