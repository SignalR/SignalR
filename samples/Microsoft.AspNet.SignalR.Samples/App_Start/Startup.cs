﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Connections;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]

namespace Microsoft.AspNet.SignalR.Samples
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //ConfigureCors(app);

            app.MapConnection<SendingConnection>("/sending-connection");
            app.MapConnection<TestConnection>("/test-connection");
            app.MapConnection<RawConnection>("/raw-connection");
            app.MapConnection<StreamingConnection>("/streaming-connection");

            app.Use(typeof(ClaimsMiddleware));

            ConfigureSignalR(GlobalHost.DependencyResolver, GlobalHost.HubPipeline);

            var config = new HubConfiguration()
            {
                EnableDetailedErrors = true
            };

            app.MapHubs(config);

            app.Map("/basicauth", map =>
            {
                map.UseBasicAuthentication(new BasicAuthenticationProvider());
                map.MapConnection<AuthenticatedEchoConnection>("/echo");
                map.MapHubs();
            });

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