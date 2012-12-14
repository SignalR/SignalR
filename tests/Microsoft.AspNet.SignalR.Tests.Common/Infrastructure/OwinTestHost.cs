using System;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.Owin.Hosting;
using Owin;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class OwinTestHost : ITestHost
    {
        private IDisposable _server;
        // private IDependencyResolver _resolver;

        private static Random _random = new Random();

        public OwinTestHost()
        {
            Url = "http://localhost:" + _random.Next(8000, 9000);
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

        public void Start<TApplication>()
        {
            Initialize();

            // REVIEW: We don't support specifying settings today. Since Configuration gets called
            // on a new instance.
            _server = WebApplication.Start<TApplication>(Url);
        }

        public void Initialize(int? keepAlive = 15, int? connectionTimeout = 120, int? disconnectTimeout = 40, int? hearbeatInterval = 10, bool enableAutoRejoiningGroups = false)
        {            
            //_resolver = new DefaultDependencyResolver();

            //var configurationManager = _resolver.Resolve<IConfigurationManager>();

            //if (keepAlive != null)
            //{
            //    configurationManager.KeepAlive = TimeSpan.FromSeconds(keepAlive.Value);
            //}
            //if (connectionTimeout != null)
            //{
            //    configurationManager.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout.Value);
            //}
            //if (disconnectTimeout != null)
            //{
            //    configurationManager.DisconnectTimeout = TimeSpan.FromSeconds(disconnectTimeout.Value);
            //}
            //if (hearbeatInterval != null)
            //{
            //    configurationManager.HeartbeatInterval = TimeSpan.FromSeconds(hearbeatInterval.Value);
            //}

            //if (enableAutoRejoiningGroups)
            //{
            //    _resolver.Resolve<IHubPipeline>().EnableAutoRejoiningGroups();
            //}
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
        }
    }
}
