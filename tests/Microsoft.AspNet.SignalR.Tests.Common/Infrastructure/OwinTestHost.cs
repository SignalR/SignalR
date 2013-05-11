using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.Owin.Hosting;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class OwinTestHost : ITestHost
    {
        private IDisposable _server;

        private static Random _random = new Random();

        public OwinTestHost()
        {
            Url = "http://localhost:" + _random.Next(8000, 9000);
            Disposables = new List<IDisposable>();
            ExtraData = new Dictionary<string, string>();
        }

        public string Url
        {
            get;
            private set;
        }

        public IClientTransport Transport
        {
            get;
            set;
        }

        public Func<IClientTransport> TransportFactory
        {
            get;
            set;
        }

        public TextWriter ClientTraceOutput
        {
            get;
            set;
        }

        public IList<IDisposable> Disposables
        {
            get;
            private set;
        }

        public IDependencyResolver Resolver { get; set; }

        public IDictionary<string, string> ExtraData
        {
            get;
            private set;
        }

        public void Initialize(int? keepAlive = -1, int? connectionTimeout = 110, int? disconnectTimeout = 30, bool enableAutoRejoiningGroups = false)
        {
            var resolver = new DefaultDependencyResolver();

            var configuration = resolver.Resolve<IConfigurationManager>();

            if (!keepAlive.HasValue)
            {
                configuration.KeepAlive = null;
            }
            // Set only if the keep-alive was changed from the default value.
            else if (keepAlive.Value != -1)
            {
                configuration.KeepAlive = TimeSpan.FromSeconds(keepAlive.Value);
            }

            if (connectionTimeout != null)
            {
                configuration.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout.Value);
            }

            if (disconnectTimeout != null)
            {
                configuration.DisconnectTimeout = TimeSpan.FromSeconds(disconnectTimeout.Value);
            }

            _server = WebApp.Start(Url, app =>
            {
                Initializer.ConfigureRoutes(app, resolver);
            });
        }

        public Task Get(string uri, bool disableWrites)
        {
            throw new NotImplementedException();
        }

        public Task Post(string uri, IDictionary<string, string> data)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_server != null)
            {
                _server.Dispose();
            }

            foreach (var d in Disposables)
            {
                d.Dispose();
            }
        }
    }
}
