using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.Samples.Raw;
using Microsoft.AspNet.SignalR.Samples.Streaming;
using Owin;

namespace Microsoft.AspNet.SignalR.Samples
{
    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapConnection<SendingConnection>("sending-connection", "sending-connection");
            routes.MapConnection<TestConnection>("test-connection", "test-connection");
            routes.MapConnection<RawConnection>("raw-connection", "raw-connection");
            routes.MapConnection<StreamingConnection>("streaming-connection", "streaming-connection");

            // Register the default hubs route /signalr
            routes.MapHubs("/signalr", new HubConfiguration() { EnableDetailedErrors = true }, AuthMiddleware);
        }

        private static void AuthMiddleware(IAppBuilder app)
        {
            Func<AppFunc, AppFunc> middleware = (next) =>
            {
                return env =>
                {
                    var headers = (IDictionary<string, string[]>)env["owin.RequestHeaders"];
                    string[] username;
                    if (headers.TryGetValue("username", out username))
                    {
                        var authenticated = (username[0] == "john") ? "true" : "false";

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Authentication, authenticated)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims);
                        env["server.User"] = new ClaimsPrincipal(claimsIdentity);
                    }
                    return next(env);
                };
            };

            app.Use(middleware);
        }
    }
}