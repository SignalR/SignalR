using System;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public abstract class HostedTest : IDisposable
    {
        protected ITestHost CreateHost(HostType hostType, TransportType transportType)
        {
            ITestHost host = null;

            switch (hostType)
            {
                case HostType.IIS:
                    host = new IISTestHost();
                    host.Transport = CreateTransport(transportType);
                    break;
                case HostType.Memory:
                    var mh = new MemoryHost();
                    host = new MemoryTestHost(mh);
                    host.Transport = CreateTransport(transportType, mh);
                    break;
                default:
                    break;
            }

            return host;
        }

        protected IClientTransport CreateTransport(TransportType transportType)
        {
            return CreateTransport(transportType, new DefaultHttpClient());
        }

        protected IClientTransport CreateTransport(TransportType transportType, IHttpClient client)
        {
            switch (transportType)
            {
                case TransportType.Websockets:
                    break;
                case TransportType.ServerSentEvents:
                    return new ServerSentEventsTransport(client);
                case TransportType.ForeverFrame:
                    break;
                case TransportType.LongPolling:
                    return new LongPollingTransport(client);
                default:
                    break;
            }

            throw new NotSupportedException("Transport not supported");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
        }
    }
}
