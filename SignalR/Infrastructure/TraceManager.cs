using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SignalR.Infrastructure
{
    public class TraceManager : ITraceManager
    {
        private readonly ConcurrentDictionary<string, TraceSource> _sources = new ConcurrentDictionary<string, TraceSource>(StringComparer.OrdinalIgnoreCase);

        public TraceManager()
        {
            Switch = new SourceSwitch("SignalRSwitch");
        }

        public SourceSwitch Switch { get; private set; }
        public TraceSource Source { get; private set; }

        public TraceSource this[string name]
        {
            get
            {
                return _sources.GetOrAdd(name, key => new TraceSource(key, SourceLevels.Off)
                {
                    Switch = Switch
                });
            }
        }
    }
}