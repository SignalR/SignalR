using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Tracing;
using Owin;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class MemoryTestHost : ITestHost
    {
        private readonly MemoryHost _host;
        private readonly TextWriterTraceListener _listener;
        private ITraceManager _traceManager;

        private static string[] _traceSources = new[] {
            "SignalR.Transports.WebSocketTransport",
            "SignalR.Transports.ServerSentEventsTransport",
            "SignalR.Transports.ForeverFrameTransport",
            "SignalR.Transports.LongPollingTransport",
            "SignalR.Transports.TransportHeartBeat"
        };

        public MemoryTestHost(MemoryHost host, string logPath)
        {
            _host = host;
            _listener = new TextWriterTraceListener(logPath + ".transports.log");
            Disposables = new List<IDisposable>();
            ExtraData = new Dictionary<string, string>();
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

        public TextWriter ClientTraceOutput { get; set; }

        public IDictionary<string, string> ExtraData { get; private set; }

        public IList<IDisposable> Disposables
        {
            get;
            private set;
        }

        public void Initialize(int? keepAlive,
                               int? connectionTimeout,
                               int? disconnectTimeout,
                               bool enableAutoRejoiningGroups)
        {
            var dr = new DefaultDependencyResolver();
            _traceManager = dr.Resolve<ITraceManager>();
            _traceManager.Switch.Level = SourceLevels.Verbose;

            foreach (var sourceName in _traceSources)
            {
                TraceSource source = _traceManager[sourceName];
                source.Listeners.Add(_listener);
            }

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
                app.MapConnection<AddGroupOnConnectedConnection>("/add-group", config);
                app.MapConnection<UnusableProtectedConnection>("/protected", config);
            });
        }

        public void Dispose()
        {
            _host.Dispose();

            foreach (var sourceName in _traceSources)
            {
                _traceManager[sourceName].Listeners.Remove(_listener);
            }

            _listener.Dispose();

            foreach (var d in Disposables)
            {
                d.Dispose();
            }
        }

        public void Shutdown()
        {
            Dispose();
        }
    }
}
