using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.Test
{
    public class NodeEvent
    {
        private static long counter = 0;

        public NodeEvent(object data)
        {
            Data = data;
            Tag = string.Format("{0}_{1}_{2}", Dns.GetHostEntry(string.Empty).HostName, Process.GetCurrentProcess().Id, Interlocked.Increment(ref counter));
        }

        public object Data { get; set; }
        public string Tag { get; set; }
    }
}