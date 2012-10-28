using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR.Hosting.Common;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    internal static class Utility
    {
        public static void InitializePerformanceCounters(IDependencyResolver resolver, CancellationToken cancellationToken)
        {
            resolver.InitializePerformanceCounters(Process.GetCurrentProcess().GetUniqueInstanceName(cancellationToken), cancellationToken);
        }
    }
}
