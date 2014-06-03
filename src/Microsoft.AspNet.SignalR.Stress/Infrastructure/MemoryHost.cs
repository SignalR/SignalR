using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.Owin.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class MemoryHost : ITestHost
    {
        private readonly TestServer _testServer;
        private readonly IHttpClient _client;
        private readonly TransportType _transportType;
        private bool _disposed;

        public MemoryHost(TransportType transport)
        {
            _testServer = TestServer.Create<Startup>();
            _client = new MemoryClient(_testServer.Handler);
            _transportType = transport;

            _disposed = false;
        }

        string ITestHost.Url { get { return "http://NotARealUrl"; } }

        void ITestHost.Initialize(int? keepAlive,
            int? connectionTimeout,
            int? disconnectTimeout,
            int? transportConnectTimeout,
            int? maxIncomingWebSocketMessageSize,
            bool enableAutoRejoiningGroups)
        {
            _client.Initialize(null);

            (this as ITestHost).TransportFactory = () =>
            {
                switch (_transportType)
                {
                    case TransportType.Websockets:
                        return new WebSocketTransport(_client);
                    case TransportType.ServerSentEvents:
                        return new ServerSentEventsTransport(_client);
                    case TransportType.ForeverFrame:
                        break;
                    case TransportType.LongPolling:
                        return new LongPollingTransport(_client);
                    default:
                        return new AutoTransport(_client);
                }

                throw new NotSupportedException("Transport not supported");
            };
        }

        Task<IResponse> ITestHost.Get(string uri)
        {
            return _client.Get(uri, r => { }, isLongRunning: false);
        }

        Task<IResponse> ITestHost.Post(string uri, IDictionary<string, string> data)
        {
            return _client.Post(uri, r => { }, data, isLongRunning: false);
        }

        IDependencyResolver ITestHost.Resolver { get; set; }

        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                _testServer.Dispose();
                _disposed = true;
            }
        }

        Func<IClientTransport> ITestHost.TransportFactory { get; set; }
    }
}
