using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public interface ITraceManager
    {
        SourceSwitch Switch { get; }
        TraceSource this[string name] { get; }
    }
}
