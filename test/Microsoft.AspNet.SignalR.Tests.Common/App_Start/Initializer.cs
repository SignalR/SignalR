// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.StressServer.Connections;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Connections;
using Microsoft.AspNet.SignalR.Tests.Common.Handlers;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json;
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

            var attachToPreSendRequestHeadersRaw = ConfigurationManager.AppSettings["attachToPreSendRequestHeaders"];

            // It is too late to add a module in the Configuration method, so we are adding it here if necessary.
            bool attachToPreSendRequestHeaders;
            if (Boolean.TryParse(attachToPreSendRequestHeadersRaw, out attachToPreSendRequestHeaders) && attachToPreSendRequestHeaders)
            {
                HttpApplication.RegisterModule(typeof(PreSendRequestHeadersModule));
            }
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

        public static void ConfigureRoutes(IAppBuilder app, IDependencyResolver resolver, string azureSignalRConnectionString = null)
        {
            var hubConfig = new HubConfiguration
            {
                Resolver = resolver,
                EnableDetailedErrors = true
            };

            if (!string.IsNullOrEmpty(azureSignalRConnectionString))
            {
#if AZURE_SIGNALR
                // We can't register all the other SignalR endpoints when testing with Azure SignalR.
                app.Map("/signalr", subapp =>
                {
                    subapp.RunAzureSignalR(typeof(Initializer).FullName, azureSignalRConnectionString, hubConfig);
                });
#else
                throw new NotSupportedException("Cannot use Azure SignalR unless the tests were built with the AzureSignalRTests MSBuild property set to 'true'.");
#endif
            }
            else
            {
                RegisterSignalREndpoints(app, resolver, hubConfig);
            }

            // Simulated ASP.NET Core SignalR negotiate endpoint
            app.Use((context, next) =>
            {
                if(context.Request.Path.StartsWithSegments(new PathString("/aspnetcore-signalr")))
                {
                    // Send an ASP.NET Core SignalR negotiate response
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    using (var writer = new JsonTextWriter(new StreamWriter(context.Response.Body)))
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("connectionId");
                        writer.WriteValue("fakeConnectionId");
                        writer.WritePropertyName("availableTransports");
                        writer.WriteStartArray();
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }
                    return Task.CompletedTask;
                }
                return next();
            });

            // Handle negotiate "Error" field
            app.Use((context, next) =>
            {
                if(context.Request.Path.StartsWithSegments(new PathString("/negotiate-error")))
                {
                    // Send an error response
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    using (var writer = new JsonTextWriter(new StreamWriter(context.Response.Body)))
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("ProtocolVersion");
                        writer.WriteValue("2.0");
                        writer.WritePropertyName("Error");
                        writer.WriteValue("Server-provided negotiate error message!");
                        writer.WriteEndObject();
                    }
                    return Task.CompletedTask;
                }
                return next();
            });

            // Redirectors:

            // Valid redirect chain
            // Overload detection doesn't like it when we use this as an extension method
            // We *intentionally* use paths that do NOT end in a trailing '/' in some places as the client needs to support both
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect", "/redirect2"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect2", "/redirect3/"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect3", "/redirect4"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect4", "/signalr/"));

            // Redirect with query string
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-query-string", "/redirect-query-string2?name1=value1&name2=value2"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-query-string2", "/signalr?name1=newValue&name3=value3"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-query-string-clear", "/redirect-query-string-clear2?clearedName=clearedValue"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-query-string-clear2", "/signalr"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-query-string-invalid", "/signalr?redirect=invalid&/?=/&"));

            // Looping redirect chain
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-loop", "/redirect-loop2"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-loop2", "/redirect-loop"));

            // Wrong protocol version
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-old-proto", "/signalr", protocolVersion: "1.5"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-new-proto", "/signalr", protocolVersion: "2.1"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-future-proto", "/signalr", protocolVersion: "2.2"));
        }

        private static void RegisterSignalREndpoints(IAppBuilder app, IDependencyResolver resolver, HubConfiguration hubConfig)
        {
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

            app.Map("/echo", map =>
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
            app.MapSignalR<AsyncOnConnectedConnection>("/async-on-connected", config);

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
                var startReceived = new ManualResetEvent(false);

                map.Use((context, next) =>
                {
                    if (context.Request.QueryString.Value.Contains("transport=longPolling"))
                    {
                        // To test reconnect scenarios we need to make sure that the transport is 
                        // successfully started before we disconnect the client. For long polling
                        // this means we need to make sure that we don't break the poll request
                        // before we send a response to the start request. Note that the first poll 
                        // request is likely to arrive before the start request. The assumption here
                        // is that there is only one active long polling connection at a time.
                        if (context.Request.Path.Value.Contains("/connect"))
                        {
                            // a new connection was started
                            startReceived.Reset();
                        }
                        else if (context.Request.Path.Value.Contains("/start"))
                        {
                            // unblock breaking the poll after start request
                            return next().ContinueWith(t => startReceived.Set());
                        }
                        else if (context.Request.Path.Value.Contains("/poll"))
                        {
                            return Task.Run(async () =>
                            {
                                // don't break the poll until start request is handled or a timeout 
                                // if it is a subsequent poll
                                startReceived.WaitOne(3000);
                                // give the start request some additional head start
                                await Task.Delay(100);
                                //subsequent long polling request should not break immediately
                                startReceived.Reset();
                                context.Response.StatusCode = 500;
                                return TaskAsyncHelper.Empty;
                            });
                        }
                    }

                    return next();
                });
                map.MapSignalR<ExamineReconnectPath>("/examine-reconnect", config);
                map.MapSignalR(hubConfig);
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

        private static Func<IOwinContext, Func<Task>, Task> CreateRedirector(string sourcePath, string targetPath, string protocolVersion = null)
        {
            return (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(new PathString(sourcePath)))
                {
                    if (context.Request.Path.StartsWithSegments(new PathString("/redirect-query-string-clear2")) &&
                        context.Request.Query["clearedName"] != "clearedValue")
                    {
                        throw new Exception("Client didn't include query string from the RedirectUrl returned by /redirect-query-string-clear.");
                    }

                    var redirectUrl = $"{context.Request.Scheme}://{context.Request.Host.Value}{targetPath}";

                    if (context.Request.Path.StartsWithSegments(new PathString("/redirect-query-string2")) &&
                        !string.IsNullOrEmpty(context.Request.Query["name1"]))
                    {
                        // We're running a test where redirect targetPath should already include a query string with a "name1" value.
                        redirectUrl += "&origName1=" + context.Request.Query["name1"];
                    }

                    // Send a redirect response
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    using (var writer = new JsonTextWriter(new StreamWriter(context.Response.Body)))
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("ProtocolVersion");

                        // Redirect results are always protocol 2.0, even if the client requested a different protocol.
                        writer.WriteValue(protocolVersion ?? "2.0");

                        writer.WritePropertyName("RedirectUrl");
                        writer.WriteValue(redirectUrl);
                        writer.WritePropertyName("AccessToken");
                        writer.WriteValue("TestToken");
                        writer.WriteEndObject();
                    }
                    return Task.CompletedTask;
                }
                return next();
            };
        }
    }
}
