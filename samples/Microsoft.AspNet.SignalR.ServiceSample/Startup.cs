using System.Threading.Tasks;
using System.Web.Configuration;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Microsoft.AspNet.SignalR.ServiceSample.Startup))]

namespace Microsoft.AspNet.SignalR.ServiceSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            if (!string.IsNullOrEmpty(WebConfigurationManager.ConnectionStrings["Azure:SignalR:ConnectionString"]?.ConnectionString))
            {
                app.Use((context, next) => ServerInfoMiddleware(context, next, usingAzureSignalR: true));
                app.MapAzureSignalR(typeof(Startup).FullName);
            }
            else
            {
                app.Use((context, next) => ServerInfoMiddleware(context, next, usingAzureSignalR: false));
                app.MapSignalR();
            }
        }

        private static async Task ServerInfoMiddleware(IOwinContext context, System.Func<Task> next, bool usingAzureSignalR)
        {
            if (context.Request.Path.StartsWithSegments(new PathString("/server-info")))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/javascript";
                await context.Response.WriteAsync($"window._server = {{ azureSignalR: {usingAzureSignalR.ToString().ToLowerInvariant()} }};");
            }
            else
            {
                await next();
            }
        }
    }
}
