using System;
using Microsoft.Owin.Hosting;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class OwinTestHost : TracingTestHost
    {
        private IDisposable _server;
        private static Random _random = new Random();
        private readonly string _url;

        public OwinTestHost(string logPath)
            : base(logPath)
        {
            _url = "http://localhost:" + _random.Next(8000, 9000);
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
                                        bool enableAutoRejoiningGroups = false,
                                        MessageBusType messageBusType = MessageBusType.Default)
        {
            base.Initialize(keepAlive, connectionTimeout, disconnectTimeout, transportConnectTimeout, enableAutoRejoiningGroups, messageBusType);

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
