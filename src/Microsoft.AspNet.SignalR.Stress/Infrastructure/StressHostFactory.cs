using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class StressHostFactory
    {
        public static ITestHost CreateHost(string hostTypeName, string transportName, string testName, string url)
        {
            HostType hostType;
            if (!Enum.TryParse<HostType>(hostTypeName, true, out hostType))
            {
                // default it to Memory Host 
                hostType = HostType.Memory;
            }

            TransportType transportType;
            if (!Enum.TryParse<TransportType>(transportName, true, out transportType))
            {
                // default it to Long Polling for transport
                transportType = TransportType.LongPolling;
            }

            return CreateHost(hostType, transportType, testName, url);
        }

        public static ITestHost CreateHost(HostType hostType, TransportType transportType, string testName, string url = null)
        {
            ITestHost host = null;

            switch (hostType)
            {
                case HostType.HttpListener:
                    host = new HttpListenerHost(transportType);
                    break;
                case HostType.External:
                    host = new ExternalHost(transportType, url);
                    break;
                case HostType.Memory:
                default:
                    host = new MemoryHost(transportType);
                    break;
            }
            return host;
        }
    }
}
