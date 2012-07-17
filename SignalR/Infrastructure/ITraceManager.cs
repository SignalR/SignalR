using System.Diagnostics;

namespace SignalR.Infrastructure
{
    public interface ITraceManager
    {
        SourceSwitch Switch { get; }
        TraceSource this[string name] { get; }
    }
}
