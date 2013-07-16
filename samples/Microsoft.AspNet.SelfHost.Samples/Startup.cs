﻿using System.Diagnostics;
using System.Web.Cors;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.Owin.Cors;
using Owin;

namespace Microsoft.AspNet.SelfHost.Samples
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/raw-connection", subApp =>
            {
                subApp.UseCors(new CorsOptions
                {
                    CorsPolicy = new CorsPolicy
                    {
                        AllowAnyHeader = true,
                        AllowAnyMethod = true,
                        AllowAnyOrigin = true,
                        SupportsCredentials = true
                    }
                });

                subApp.UseConnection<RawConnection>();
            });

            app.MapHubs();

            // Turn tracing on programmatically
            GlobalHost.TraceManager.Switch.Level = SourceLevels.Information;
        }
    }
}
