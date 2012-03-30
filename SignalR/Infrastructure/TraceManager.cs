using System.Diagnostics;

namespace SignalR.Infrastructure
{
    public class TraceManager : ITraceManager
    {
        public TraceManager()
        {
            Switch = new SourceSwitch("SignalRSwitch");
            Source = new TraceSource("SignalRSource", SourceLevels.Off) { Switch = Switch };
        }

        public SourceSwitch Switch { get; private set; }
        public TraceSource Source { get; private set; }
    }
}