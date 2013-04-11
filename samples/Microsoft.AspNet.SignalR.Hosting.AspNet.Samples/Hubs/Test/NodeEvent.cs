using Microsoft.AspNet.SignalR.Tracing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.Test
{
    public class NodeEvent
    {
        static long counter = 0;

        public NodeEvent(object data)
        {
            Data = data;
            Tag = string.Format("{0}_{1}_{2}", Dns.GetHostEntry(string.Empty).HostName, Process.GetCurrentProcess().Id, Interlocked.Increment(ref counter));
        }

        public object Data { get; set; }
        public string Tag { get; set; }
    }
}