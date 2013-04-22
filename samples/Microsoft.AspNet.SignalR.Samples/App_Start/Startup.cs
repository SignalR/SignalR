using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Owin;

namespace Microsoft.AspNet.SignalR.Samples
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapConnection<SendingConnection>("sending-connection");
            app.MapConnection<TestConnection>("test-connection");
            app.MapConnection<RawConnection>("raw-connection");
            app.MapConnection<StreamingConnection>("streaming-connection");

            SetupAuthenticationMiddleware(app);

            ConfigureSignalR(GlobalHost.DependencyResolver, GlobalHost.HubPipeline);

            var config = new HubConfiguration()
            {
                EnableDetailedErrors = true
            };

            app.MapHubs(config);

            BackgroundThread.Start();
        }

        private static void SetupAuthenticationMiddleware(IAppBuilder app)
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