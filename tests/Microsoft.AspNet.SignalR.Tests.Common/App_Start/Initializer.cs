using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.StressServer.Connections;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Connections;
using Microsoft.AspNet.SignalR.Tests.Common.Handlers;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: PreApplicationStartMethod(typeof(Initializer), "Start")]
[assembly: OwinStartup(typeof(Initializer))]

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public static class Initializer
    {
        public static void Start()
        {
            RouteTable.Routes.Add("ping", new Route("ping", new PingHandler()));
            RouteTable.Routes.Add("gc", new Route("gc", new GCHandler()));

            // Add a route that enables session in the handler
            RouteTable.Routes.Add("session", new Route("session/{*path}", new HandlerWithSession()));

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
            string maxIncomingWebSocketMessageSizeRaw = ConfigurationManager.AppSettings["maxIncomingWebSocketMessageSize"];
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

            int maxIncomingWebSocketMessageSize;
            if (String.IsNullOrEmpty(maxIncomingWebSocketMessageSizeRaw))
            {
                GlobalHost.Configuration.MaxIncomingWebSocketMessageSize = null;
            }
            else if (Int32.TryParse(maxIncomingWebSocketMessageSizeRaw, out maxIncomingWebSocketMessageSize))
            {
                GlobalHost.Configuration.MaxIncomingWebSocketMessageSize = maxIncomingWebSocketMessageSize;
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
                EnableDetailedErrors = true
            };

            app.MapSignalR(hubConfig);

            app.MapSignalR("/signalr2/test", new HubConfiguration()
            {
                Resolver = resolver
            });

            var config = new ConnectionConfiguration
            {
                Resolver = resolver
            };

            app.Map("/multisend", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                map.RunSignalR<MySendingConnection>(config);
            });

            app.Map("/autoencodedjson", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                map.RunSignalR<EchoConnection>(config);
            });

            app.Map("/redirectionConnection", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                map.RunSignalR<RedirectionConnection>(config);
            });

            app.Map("/statusCodeConnection", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                map.RunSignalR<StatusCodeConnection>(config);
            });

            app.Map("/jsonp", map =>
            {
                var jsonpConfig = new ConnectionConfiguration
                {
                    Resolver = resolver,
                    EnableJSONP = true
                };

                map.MapSignalR<EchoConnection>("/echo", jsonpConfig);

                var jsonpHubsConfig = new HubConfiguration
                {
                    Resolver = resolver,
                    EnableJSONP = true
                };

                map.MapSignalR(jsonpHubsConfig);
            });

            app.MapSignalR<MyBadConnection>("/ErrorsAreFun", config);
            app.MapSignalR<MyGroupEchoConnection>("/group-echo", config);
            app.MapSignalR<MyReconnect>("/my-reconnect", config);
            app.MapSignalR<ExamineHeadersConnection>("/examine-request", config);
            app.MapSignalR<ExamineReconnectPath>("/examine-reconnect", config);
            app.MapSignalR<MyGroupConnection>("/groups", config);
            app.MapSignalR<MyRejoinGroupsConnection>("/rejoin-groups", config);
            app.MapSignalR<BroadcastConnection>("/filter", config);
            app.MapSignalR<ConnectionThatUsesItems>("/items", config);
            app.MapSignalR<SyncErrorConnection>("/sync-error", config);
            app.MapSignalR<AddGroupOnConnectedConnection>("/add-group", config);
            app.MapSignalR<UnusableProtectedConnection>("/protected", config);
            app.MapSignalR<FallbackToLongPollingConnectionThrows>("/fall-back-throws", config);
            app.MapSignalR<PreserializedJsonConnection>("/preserialize", config);

            // This subpipeline is protected by basic auth
            app.Map("/basicauth", map =>
            {
                map.Use(async (context, next) =>
                {
                    var authorization = context.Request.Headers.Get("Authorization");
                    if (string.IsNullOrEmpty(authorization))
                    {
                        context.Response.StatusCode = 401;
                        context.Response.Headers.Add("WWW-Authenticate", new string[] { "Basic" });
                    }
                    else
                    {
                        var base64Encoded = authorization.Replace("Basic ", "");
                        byte[] base64EncodedBytes = Convert.FromBase64String(base64Encoded);
                        var base64Decoded = System.Text.ASCIIEncoding.ASCII.GetString(base64EncodedBytes);
                        var credentials = base64Decoded.Split(':');
                        var identity = new ClaimsIdentity("Basic");
                        identity.AddClaim(new Claim(ClaimTypes.Name, credentials[0]));
                        context.Request.User = new ClaimsPrincipal(identity);
                        await next();
                    }
                });

                var subConfig = new ConnectionConfiguration
                {
                    Resolver = resolver
                };

                map.MapSignalR<AuthenticatedEchoConnection>("/echo", subConfig);

                var subHubsConfig = new HubConfiguration
                {
                    Resolver = resolver
                };

                map.MapSignalR(subHubsConfig);
            });

            // This subpipeline is protected by cookie auth
            app.Map("/cookieauth", map =>
            {
                var options = new CookieAuthenticationOptions()
                {
                    AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                    LoginPath = CookieAuthenticationDefaults.LoginPath,
                    LogoutPath = CookieAuthenticationDefaults.LogoutPath,
                };

                map.UseCookieAuthentication(options);

                map.Use(async (context, next) =>
                {
                    if (context.Request.Path.Value.Contains(options.LoginPath.Value))
                    {
                        if (context.Request.Method == "POST")
                        {
                            var form = await context.Request.ReadFormAsync();
                            var userName = form["UserName"];
                            var password = form["Password"];

                            var identity = new ClaimsIdentity(options.AuthenticationType);
                            identity.AddClaim(new Claim(ClaimTypes.Name, userName));
                            context.Authentication.SignIn(identity);
                        }
                    }
                    else
                    {
                        await next();
                    }
                });

                var subConfig = new ConnectionConfiguration
                {
                    Resolver = resolver
                };

                map.MapSignalR<AuthenticatedEchoConnection>("/echo", subConfig);

                var subHubsConfig = new HubConfiguration
                {
                    Resolver = resolver
                };

                map.MapSignalR(subHubsConfig);
            });

            app.Map("/fall-back", map =>
            {
                map.Use((context, next) =>
                {
                    if (!context.Request.Path.Value.Contains("negotiate") &&
                        !context.Request.QueryString.Value.Contains("longPolling"))
                    {
                        context.Response.Body = new MemoryStream();
                    }

                    return next();
                });

                map.RunSignalR<FallbackToLongPollingConnection>();
            });

            app.Map("/no-init", map =>
            {
                map.Use((context, next) =>
                {
                    if (context.Request.Path.Value.Contains("connect"))
                    {
                        context.Response.Body = new MemoryStream();
                    }

                    return next();
                });
            });

            app.Map("/force-lp-reconnect", map =>
            {
                map.Use((context, next) =>
                {
                    if (context.Request.Path.Value.Contains("poll"))
                    {
                        context.Response.StatusCode = 500;
                        return TaskAsyncHelper.Empty;
                    }

                    return next();
                });
                map.MapSignalR<ExamineReconnectPath>("/examine-reconnect", config);
                map.MapSignalR(hubConfig);
            });

            var longPollDelayResolver = new DefaultDependencyResolver();
            var configManager = longPollDelayResolver.Resolve<IConfigurationManager>();

            configManager.LongPollDelay = TimeSpan.FromSeconds(60);
            // Make the disconnect timeout and the keep alive interval short so we can
            // complete our tests quicker.
            configManager.DisconnectTimeout = TimeSpan.FromSeconds(6);

            app.MapSignalR<EchoConnection>("/longPollDelay", new ConnectionConfiguration
            {
                Resolver = longPollDelayResolver
            });

            // Perf/stress test related
            var performanceConfig = new ConnectionConfiguration
            {
                Resolver = resolver
            };

            app.MapSignalR<StressConnection>("/echo", performanceConfig);

            performanceConfig.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());

            // IMPORTANT: This needs to run last so that it runs in the "default" part of the pipeline

            // Session is enabled for ASP.NET on the session path
            app.Map("/session", map =>
            {
                map.MapSignalR();
            });
        }
    }
}
