using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SignalR.Hubs;

namespace SignalR.Hosting.AspNet.Samples.Hubs.KeepAlive
{
    public class KeepAlive : Hub, IConnected, IDisconnect
    {
        public void SetKeepAlive(int seconds)
        {
            GlobalHost.Configuration.KeepAlive = new TimeSpan(0, 0, seconds);    
        }

        public Task Connect()
        {
            return null;
        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            return null;
        }

        public Task Disconnect()
        {
            return null;
        }
    }
}