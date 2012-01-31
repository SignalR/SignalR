using System;

namespace SignalR
{
    public static class ConnectionScope
    {
        public static readonly string Global = typeof(PersistentConnection).FullName;
        // TODO: Come up with something here
        // public static readonly string Machine = typeof(PersistentConnection).FullName;
        public static readonly string AppDomain = Guid.NewGuid().ToString();
    }
}
