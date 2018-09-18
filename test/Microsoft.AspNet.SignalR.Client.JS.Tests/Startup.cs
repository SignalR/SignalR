// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Cors;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace Microsoft.AspNet.SignalR.Client.JS.Tests
{
    public class Startup
    {
        // Super hacky settings!
        public static string AzureSignalRConnectionString = null;
        public void Configuration(IAppBuilder app)
        {
            var corsPolicy = new CorsPolicy()
            {
                AllowAnyHeader = true,
                AllowAnyMethod = true,
                AllowAnyOrigin = true,
                SupportsCredentials = true,
            };

            app.UseErrorPage();

            // Enable CORS so Karma works
            app.UseCors(new CorsOptions()
            {
                PolicyProvider = new CorsPolicyProvider()
                {
                    PolicyResolver = context => Task.FromResult(corsPolicy),
                }
            });

            Initializer.ConfigureRoutes(app, GlobalHost.DependencyResolver, AzureSignalRConnectionString);

            app.Use((context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(new PathString("/status")))
                {
                    context.Response.StatusCode = 200;
                    return Task.CompletedTask;
                }
                return next();
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(new PathString("/js/server-info.js")))
                {
                    // Inject server settings in to the javascript
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/javascript";
                    await context.Response.WriteAsync($"window._server = {{ azureSignalR: {(string.IsNullOrEmpty(AzureSignalRConnectionString) ? "false" : "true")} }}");
                }
                else
                {
                    await next();
                }
            });

            app.UseFileServer(new FileServerOptions()
            {
                EnableDefaultFiles = true,
                FileSystem = new PhysicalFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
            });
        }
    }
}
