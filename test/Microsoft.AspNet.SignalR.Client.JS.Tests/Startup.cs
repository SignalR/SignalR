// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
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

            app.UseFileServer(new FileServerOptions()
            {
                EnableDefaultFiles = true,
                FileSystem = new PhysicalFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
            });
        }
    }
}
