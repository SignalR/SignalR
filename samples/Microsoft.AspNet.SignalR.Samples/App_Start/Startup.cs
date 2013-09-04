using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Connections;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
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

            var config = new HubConfiguration()
            {
                EnableDetailedErrors = true
            };

            app.MapSignalR(config);

            app.Map("/cors", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                map.MapSignalR<RawConnection>("/raw-connection");
                map.MapSignalR();
            });

            app.Map("/basicauth", map =>
            {
                map.UseBasicAuthentication(new BasicAuthenticationProvider());
                map.MapSignalR<AuthenticatedEchoConnection>("/echo");
                map.MapSignalR();
            });

            app.Map("/streaming-api", map =>
            {
                // Streaming API using SignalR
                var connectionContext = GlobalHost.ConnectionManager.GetConnectionContext<RawConnection>();

                map.Run(async context =>
                {
                    var tcs = new TaskCompletionSource<object>();

                    // Whenever a message is sent to this connection we're going to write it to the response
                    IDisposable disposable = connectionContext.Connection.Receive(async message =>
                    {
                        await context.Response.WriteAsync(message.ToString());
                        await context.Response.Body.FlushAsync();
                    });

                    context.Request.CallCancelled.Register(() => tcs.TrySetResult(null));

                    await tcs.Task;

                    disposable.Dispose();
                });

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