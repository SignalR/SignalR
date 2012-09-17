using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SignalR
{
    public class ScaleoutMapping
    {
        public ConcurrentDictionary<string, LocalEventKeyInfo> EventKeyMappings { get; private set; }

        public ScaleoutMapping(IDictionary<string, LocalEventKeyInfo> mappings)
        {
            EventKeyMappings = new ConcurrentDictionary<string, LocalEventKeyInfo>(mappings, StringComparer.OrdinalIgnoreCase);
        }
    }
}
