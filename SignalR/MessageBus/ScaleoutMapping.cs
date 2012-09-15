using System;
using System.Collections.Concurrent;

namespace SignalR
{
    public class ScaleoutMapping
    {
        public ConcurrentDictionary<string, LocalEventKeyInfo> EventKeyMappings { get; private set; }

        public ScaleoutMapping()
        {
            EventKeyMappings = new ConcurrentDictionary<string, LocalEventKeyInfo>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
