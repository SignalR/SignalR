using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Cors;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json;
using Owin;

namespace Microsoft.AspNet.SignalR.Client.JS.Tests
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var corsPolicy = new CorsPolicy()
            {
                AllowAnyHeader = true,
                AllowAnyMethod = true,
                AllowAnyOrigin = true,
                SupportsCredentials = true,
            };

            // Enable CORS so Karma works
            app.UseCors(new CorsOptions()
            {
                PolicyProvider = new CorsPolicyProvider()
                {
                    PolicyResolver = context => Task.FromResult(corsPolicy),
                }
            });

            Initializer.ConfigureRoutes(app, GlobalHost.DependencyResolver);

            app.Use((context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(new PathString("/status")))
                {
                    context.Response.StatusCode = 200;
                    return Task.CompletedTask;
                }
                return next();
            });

            // Valid redirect chain
            // Overload detection doesn't like it when we use this as an extension method
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect", "/redirect2"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect2", "/redirect3"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect3", "/redirect4"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect4", "/signalr"));

            // Looping redirect chain
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-loop", "/redirect-loop2"));
            AppBuilderUseExtensions.Use(app, CreateRedirector("/redirect-loop2", "/redirect-loop"));

            app.UseFileServer(new FileServerOptions()
            {
                EnableDefaultFiles = true,
                FileSystem = new PhysicalFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
            });
        }

        private static Func<IOwinContext, Func<Task>, Task> CreateRedirector(string sourcePath, string targetPath)
        {
            return (context, next) =>
            {
                if(context.Request.Path.StartsWithSegments(new PathString(sourcePath)))
                {
                    // Send a redirect response
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    using(var writer = new JsonTextWriter(new StreamWriter(context.Response.Body)))
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("ProtocolVersion");
                        writer.WriteValue("2.0");
                        writer.WritePropertyName("RedirectUrl");
                        writer.WriteValue($"{context.Request.Scheme}://{context.Request.Host.Value}{targetPath}");
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
