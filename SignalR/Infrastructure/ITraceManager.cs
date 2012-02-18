using System.Diagnostics;

namespace SignalR.Infrastructure
{
    public interface ITraceManager
    {
        SourceSwitch Switch { get; }
        TraceSource Source { get; }
    }
}
