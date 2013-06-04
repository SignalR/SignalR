using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.StressServer.Connections;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Connections;
using Microsoft.AspNet.SignalR.Tests.Common.Handlers;
using Owin;

[assembly: PreApplicationStartMethod(typeof(Initializer), "Start")]

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public static class Initializer
    {
        public static void Start()
        {
            RouteTable.Routes.Add("ping", new Route("ping", new PingHandler()));
            RouteTable.Routes.Add("gc", new Route("gc", new GCHandler()));

            string logFileName = Path.Combine(HttpRuntime.AppDomainAppPath, ConfigurationManager.AppSettings["logFileName"] + ".server.trace.log");
            Trace.Listeners.Add(new TextWriterTraceListener(logFileName));
            Trace.AutoFlush = true;

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Trace.TraceError("Unobserved task exception: " + e.Exception.GetBaseException());

                e.SetObserved();
            };
        }

        public static void Configuration(IAppBuilder app)
        {
            string keepAliveRaw = ConfigurationManager.AppSettings["keepAlive"];
            string connectionTimeoutRaw = ConfigurationManager.AppSettings["connectionTimeout"];
            string transportConnectTimeoutRaw = ConfigurationManager.AppSettings["transportConnectTimeout"];
            string disconnectTimeoutRaw = ConfigurationManager.AppSettings["disconnectTimeout"];

            int connectionTimeout;
            if (Int32.TryParse(connectionTimeoutRaw, out connectionTimeout))
            {
                GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout);
            }

            int disconnectTimeout;
            if (Int32.TryParse(disconnectTimeoutRaw, out disconnectTimeout))
            {
                GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(disconnectTimeout);
            }

            int transportConnectTimeout;
            if (Int32.TryParse(transportConnectTimeoutRaw, out transportConnectTimeout))
            {
                GlobalHost.Configuration.TransportConnectTimeout = TimeSpan.FromSeconds(transportConnectTimeout);
            }

            int keepAlive;
            if (String.IsNullOrEmpty(keepAliveRaw))
            {
                GlobalHost.Configuration.KeepAlive = null;
            }
            // Set only if the keep-alive was changed from the default value.
            else if (Int32.TryParse(keepAliveRaw, out keepAlive) && keepAlive != -1)
            {
                GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(keepAlive);
            }

            ConfigureRoutes(app, GlobalHost.DependencyResolver);
        }

        public static void ConfigureRoutes(IAppBuilder app, IDependencyResolver resolver)
        {
            var hubConfig = new HubConfiguration
            {
                Resolver = resolver,
                EnableCrossDomain = true,
                EnableDetailedErrors = true
            };

            app.MapHubs(hubConfig);

            app.MapHubs("signalr2/test", new HubConfiguration()
            {
                Resolver = resolver
            });

            var crossDomainConfig = new ConnectionConfiguration
            {
                EnableCrossDomain = true,
                Resolver = resolver
            };

            app.MapConnection<MySendingConnection>("multisend", crossDomainConfig);

            var config = new ConnectionConfiguration
            {
                Resolver = resolver
            };

            app.MapConnection<MyBadConnection>("ErrorsAreFun", config);
            app.MapConnection<MyGroupEchoConnection>("group-echo", config);
            app.MapConnection<MyReconnect>("my-reconnect", config);
            app.MapConnection<ExamineHeadersConnection>("examine-request", config);
            app.MapConnection<ExamineReconnectPath>("examine-reconnect", config);
            app.MapConnection<MyGroupConnection>("groups", config);
            app.MapConnection<MyRejoinGroupsConnection>("rejoin-groups", config);
            app.MapConnection<FilteredConnection>("filter", config);
            app.MapConnection<ConnectionThatUsesItems>("items", config);
            app.MapConnection<SyncErrorConnection>("sync-error", config);
            app.MapConnection<AddGroupOnConnectedConnection>("add-group", config);
            app.MapConnection<UnusableProtectedConnection>("protected", config);
            app.MapConnection<FallbackToLongPollingConnection>("/fall-back", config);

            // This subpipeline is protected by basic auth
            app.MapPath("/basicauth", subApp =>
            {
                subApp.UseBasicAuthentication(new BasicAuthenticationProvider());

                var subConfig = new ConnectionConfiguration
                {
                    Resolver = resolver
                };

                subApp.MapConnection<AuthenticatedEchoConnection>("/echo", subConfig);
            });

            // Perf/stress test related
            var performanceConfig = new ConnectionConfiguration
            {
                Resolver = resolver
            };

            app.MapConnection<StressConnection>("echo", performanceConfig);

            performanceConfig.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
        }
    }
}
