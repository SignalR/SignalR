using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using Owin;

[assembly: PreApplicationStartMethod(typeof(Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS.InitializeIISHost), "Start")]

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    public static class InitializeIISHost
    {
        public static void Start()
        {
            string keepAliveRaw = ConfigurationManager.AppSettings["keepAlive"];
            string connectionTimeoutRaw = ConfigurationManager.AppSettings["connectionTimeout"];
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

            var config = new HubConfiguration
            {
                EnableCrossDomain = true,
                EnableDetailedErrors = true
            };

            RouteTable.Routes.MapHubs("signalr.hubs2", "signalr2/test", new HubConfiguration(), _ => { });
            RouteTable.Routes.MapHubs(config);

            RouteTable.Routes.MapConnection<MyBadConnection>("errors-are-fun", "ErrorsAreFun");
            RouteTable.Routes.MapConnection<MyGroupEchoConnection>("group-echo", "group-echo");
            RouteTable.Routes.MapConnection<MySendingConnection>("multisend", "multisend", new ConnectionConfiguration { EnableCrossDomain = true });
            RouteTable.Routes.MapConnection<MyReconnect>("my-reconnect", "my-reconnect");
            RouteTable.Routes.MapConnection<ExamineHeadersConnection>("examine-request", "examine-request");
            RouteTable.Routes.MapConnection<ExamineReconnectPath>("examine-reconnect", "examine-reconnect");
            RouteTable.Routes.MapConnection<MyGroupConnection>("groups", "groups");
            RouteTable.Routes.MapConnection<MyRejoinGroupsConnection>("rejoin-groups", "rejoin-groups");
            RouteTable.Routes.MapConnection<FilteredConnection>("filter", "filter");
            RouteTable.Routes.MapConnection<ConnectionThatUsesItems>("items", "items");
            RouteTable.Routes.MapConnection<SyncErrorConnection>("sync-error", "sync-error");
            RouteTable.Routes.MapConnection<AddGroupOnConnectedConnection>("add-group", "add-group");
            RouteTable.Routes.MapConnection<UnusableProtectedConnection>("protected", "protected");
            RouteTable.Routes.MapConnection<FallbackToLongPollingConnection>("fall-back", "/fall-back");


            RouteTable.Routes.Add("ping", new Route("ping", new PingHandler()));
            RouteTable.Routes.Add("gc", new Route("gc", new GCHandler()));

            string logFileName = Path.Combine(HttpRuntime.AppDomainAppPath, ConfigurationManager.AppSettings["logFileName"] + ".server.trace.log");
            Trace.Listeners.Add(new TextWriterTraceListener(logFileName));
            Trace.AutoFlush = true;

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Trace.TraceError("Unobserved task exception: " + e.Exception.GetBaseException());

            e.SetObserved();
        }

        private static void Middleware(IAppBuilder app)
        {
            Func<AppFunc, AppFunc> middleware = (next) =>
            {
                return env =>
                {
                    var headers = (IDictionary<string, string[]>)env["owin.RequestHeaders"];
                    string[] username;
                    headers.TryGetValue("username", out username);
                    var authenticated = (username[0] == "john") ? "true" : "false";

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Authentication, authenticated)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims);
                    env["server.User"] = new ClaimsPrincipal(claimsIdentity);
                    return next(env);
                };
            };

            app.Use(middleware);
        }
    }
}
