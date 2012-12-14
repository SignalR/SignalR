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
                case HostType.IISExpress:
                    host = new IISExpressTestHost();
                    host.TransportFactory = () => CreateTransport(transportType);
                    host.Transport = host.TransportFactory();
                    break;
                case HostType.Memory:
                    var mh = new MemoryHost();
                    host = new MemoryTestHost(mh);
                    host.TransportFactory = () => CreateTransport(transportType, mh);
                    host.Transport = host.TransportFactory();
                    break;
                case HostType.Owin:
                    host = new OwinTestHost();
                    host.TransportFactory = () => CreateTransport(transportType);
                    host.Transport = host.TransportFactory();
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
                    return new WebSocketTransport(client);
                case TransportType.ServerSentEvents:
                    return new ServerSentEventsTransport(client)
                    {
                        ConnectionTimeout = TimeSpan.FromSeconds(10)
                    };
                case TransportType.ForeverFrame:
                    break;
                case TransportType.LongPolling:
                    return new LongPollingTransport(client);
                default:
                    return new AutoTransport(client);
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
