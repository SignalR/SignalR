using System;
using System.Net;
using Microsoft.Owin.Hosting;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class OwinTestHost : TracingTestHost
    {
        private IDisposable _server;
        private static readonly Random Random = new Random();
        private readonly string _url;

        public OwinTestHost(string logPath)
            : base(logPath)
        {
            _url = GetUrl();
        }

        private static string GetUrl()
        {
            for (var i = 0; i < 10; i++)
            {
                var url = string.Format("http://localhost:{0}/", Random.Next(8000, 9000));
                if (ProbeUrl(url))
                {
                    return url;
                }
            }

            throw new InvalidOperationException("Could not find free port.");
        }

        private static bool ProbeUrl(string url)
        {
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add(url);
                listener.Start();
                listener.Stop();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public override string Url
        {
            get
            {
                return _url;
            }
        }

        public override void Initialize(int? keepAlive = -1,
                                        int? connectionTimeout = 110,
                                        int? disconnectTimeout = 30,
                                        int? transportConnectTimeout = 5,
                                        int? maxIncomingWebSocketMessageSize = 64 * 1024,
                                        bool enableAutoRejoiningGroups = false,
                                        MessageBusType messageBusType = MessageBusType.Default)
        {
            base.Initialize(keepAlive,
                            connectionTimeout,
                            disconnectTimeout,
                            transportConnectTimeout,
                            maxIncomingWebSocketMessageSize,
                            enableAutoRejoiningGroups,
                            messageBusType);

            _server = WebApp.Start(Url, app =>
            {
                Initializer.ConfigureRoutes(app, Resolver);
            });
        }

        public override void Dispose()
        {
            if (_server != null)
            {
                _server.Dispose();
            }

            base.Dispose();
        }
    }
}
