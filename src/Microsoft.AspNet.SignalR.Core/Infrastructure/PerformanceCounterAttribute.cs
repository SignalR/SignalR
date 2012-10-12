using System;
using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple=false)]
    internal class PerformanceCounterAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public PerformanceCounterType CounterType { get; set; }
    }
}
