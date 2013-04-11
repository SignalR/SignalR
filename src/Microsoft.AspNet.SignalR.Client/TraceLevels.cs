using System;

namespace Microsoft.AspNet.SignalR.Client
{
    [Flags]
    public enum TraceLevels
    {
        None = 0,
        Messages = 1,
        Events = 2,
        StateChanges = 4,
        All = Messages | Events | StateChanges
    }
}
