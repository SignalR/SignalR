using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class HttpListenerHost : ITestHost
    {
        private static Random _random = new Random();

        private readonly TransportType _transportType;
        private readonly string _url;
        private IDisposable _server;
        private bool _disposed;

        private Lazy<HttpClient> _client = new Lazy<HttpClient>();

        public HttpListenerHost(TransportType transportType)
        {
            _transportType = transportType;
            _url = "http://localhost:" + _random.Next(8000, 9000);
            _disposed = false;
        }

        string ITestHost.Url { get { return _url; } }

        IDependencyResolver ITestHost.Resolver { get; set; }

        void ITestHost.Initialize(int? keepAlive,
            int? connectionTimeout,
            int? disconnectTimeout,
            int? transportConnectTimeout,
            int? maxIncomingWebSocketMessageSize,
            bool enableAutoRejoiningGroups)
        {
            _server = WebApp.Start<Startup>(_url);

            (this as ITestHost).TransportFactory = () =>
            {
                switch (_transportType)
                {
                    case TransportType.Websockets:
                        return new WebSocketTransport(new DefaultHttpClient());
                    case TransportType.ServerSentEvents:
                        return new ServerSentEventsTransport(new DefaultHttpClient());
                    case TransportType.ForeverFrame:
                        break;
                    case TransportType.LongPolling:
                        return new LongPollingTransport(new DefaultHttpClient());
                    default:
                        return new AutoTransport(new DefaultHttpClient());
                }

                throw new NotSupportedException("Transport not supported");
            };
        }

        Func<IClientTransport> ITestHost.TransportFactory { get; set; }

        async Task<IResponse> ITestHost.Get(string uri)
        {
            HttpResponseMessage response = await _client.Value.GetAsync(uri);
            await response.Content.ReadAsStreamAsync();
            return new ResponseWrapper(response);
        }

        async Task<IResponse> ITestHost.Post(string uri, IDictionary<string, string> data)
        {
            HttpResponseMessage response = await _client.Value.PostAsync(uri, new FormUrlEncodedContent(data ?? new Dictionary<string, string>()));
            await response.Content.ReadAsStreamAsync();
            return new ResponseWrapper(response);
        }

        void IDisposable.Dispose()
        {
            if (!_disposed)
            {
                _client.Value.Dispose();
                _server.Dispose();
                _disposed = true;
            }
        }

        private class ResponseWrapper : IResponse
        {
            private readonly HttpResponseMessage _response;

            public ResponseWrapper(HttpResponseMessage response)
            {
                _response = response;
            }

            public Stream GetStream()
            {
                return _response.Content.ReadAsStreamAsync().Result;
            }

            public void Dispose()
            {
                _response.Dispose();
            }
        }
    }
}
