﻿using System;
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

        public void Initialize(int keepAlive,
                               int? connectionTimeout,
                               int? disconnectTimeout,
                               bool enableAutoRejoiningGroups)
        {
            var dr = new DefaultDependencyResolver();

            _host.Configure(app =>
            {
                var configuration = dr.Resolve<IConfigurationManager>();

                configuration.KeepAlive = TimeSpan.FromSeconds(keepAlive);

                if (connectionTimeout != null)
                {
                    configuration.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout.Value);
                }

                if (disconnectTimeout != null)
                {
                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(disconnectTimeout.Value);
                }

                if (enableAutoRejoiningGroups)
                {
                    dr.Resolve<IHubPipeline>().EnableAutoRejoiningGroups();
                }

                app.MapHubs("/signalr", new HubConfiguration { Resolver = dr });

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
