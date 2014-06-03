using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class ExternalHost : ITestHost
    {
        private readonly TransportType _transportType;
        private readonly string _url;
        private bool _disposed;

        private Lazy<HttpClient> _client = new Lazy<HttpClient>();

        public ExternalHost(TransportType transportType, string url)
        {
            _transportType = transportType;
            _url = url;
            _disposed = false;
        }

        IDependencyResolver ITestHost.Resolver { get; set; }

        string ITestHost.Url { get { return _url; } }

        void ITestHost.Initialize(int? keepAlive,
            int? connectionTimeout,
            int? disconnectTimeout,
            int? transportConnectTimeout,
            int? maxIncomingWebSocketMessageSize,
            bool enableAutoRejoiningGroups)
        {
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
