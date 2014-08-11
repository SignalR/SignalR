using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Connections;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace Microsoft.AspNet.SignalR.Samples
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR<SendingConnection>("/sending-connection");
            app.MapSignalR<TestConnection>("/test-connection");
            app.MapSignalR<RawConnection>("/raw-connection");
            app.MapSignalR<StreamingConnection>("/streaming-connection");

            app.Use(typeof(ClaimsMiddleware));

            ConfigureSignalR(GlobalHost.DependencyResolver, GlobalHost.HubPipeline);

            app.Map("/cors", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                map.MapSignalR<RawConnection>("/raw-connection");
                map.MapSignalR();
            });

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

                map.MapSignalR<AuthenticatedEchoConnection>("/echo");
                map.MapSignalR();
            });

            var config = new HubConfiguration()
            {
                EnableDetailedErrors = true
            };

            app.MapSignalR(config);

            BackgroundThread.Start();
        }

        private class ClaimsMiddleware : OwinMiddleware
        {
            public ClaimsMiddleware(OwinMiddleware next)
                : base(next)
            {
            }

            public override Task Invoke(IOwinContext context)
            {
                string username = context.Request.Headers.Get("username");

                if (!String.IsNullOrEmpty(username))
                {
                    var authenticated = username == "john" ? "true" : "false";

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Authentication, authenticated)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims);
                    context.Request.User = new ClaimsPrincipal(claimsIdentity);
                }

                return Next.Invoke(context);
            }
        }
    }
}