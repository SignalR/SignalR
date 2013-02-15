using System;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Configuration;
using Owin;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class MemoryTestHost : ITestHost
    {
        private readonly MemoryHost _host;

        public MemoryTestHost(MemoryHost host)
        {
            _host = host;
        }

        public string Url
        {
            get
            {
                return "http://memoryhost";
            }
        }

        public IClientTransport Transport { get; set; }

        public Func<IClientTransport> TransportFactory { get; set; }

        public void Initialize(int? keepAlive,
                               int? connectionTimeout,
                               int? disconnectTimeout,
                               bool enableAutoRejoiningGroups)
        {
            var dr = new DefaultDependencyResolver();

            _host.Configure(app =>
            {
                var configuration = dr.Resolve<IConfigurationManager>();

                if (connectionTimeout != null)
                {
                    configuration.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout.Value);
                }

                if (disconnectTimeout != null)
                {
                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(disconnectTimeout.Value);
                }

                if (!keepAlive.HasValue)
                {
                    configuration.KeepAlive = null;
                }
                // Set only if the keep-alive was changed from the default value.
                else if (keepAlive.Value != -1)
                {
                    configuration.KeepAlive = TimeSpan.FromSeconds(keepAlive.Value);
                }

                app.MapHubs("/signalr2/test", new HubConfiguration());
                app.MapHubs("/signalr", new HubConfiguration { EnableDetailedErrors = true, Resolver = dr });

                var config = new ConnectionConfiguration
                {
                    Resolver = dr
                };

                app.MapConnection<MyBadConnection>("/ErrorsAreFun", config);
                app.MapConnection<MyGroupEchoConnection>("/group-echo", config);
                app.MapConnection<MySendingConnection>("/multisend", config);
                app.MapConnection<MyReconnect>("/my-reconnect", config);
                app.MapConnection<MyGroupConnection>("/groups", config);
                app.MapConnection<MyRejoinGroupsConnection>("/rejoin-groups", config);
                app.MapConnection<FilteredConnection>("/filter", config);
                app.MapConnection<SyncErrorConnection>("/sync-error", config);
                app.MapConnection<FallbackToLongPollingConnection>("/fall-back", config);
                app.MapConnection<AddGroupOnConnectedConnection>("/add-group", config);
                app.MapConnection<UnusableProtectedConnection>("/protected", config);
            });
        }

        public void Dispose()
        {
            _host.Dispose();
        }

        public void Shutdown()
        {
            Dispose();
        }
    }
}
