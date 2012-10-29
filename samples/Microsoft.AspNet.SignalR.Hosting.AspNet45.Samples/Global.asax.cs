using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Web.Routing;
using System.Web.Security;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub;

namespace Microsoft.AspNet.SignalR.Samples
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            SignalRConfig.ConfigureSignalR(GlobalHost.DependencyResolver, GlobalHost.HubPipeline);
            BackgroundThread.Start();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                var principal = Context.Cache[authTicket.Name] as IPrincipal;
                if (!authTicket.Expired && principal != null)
                {
                    Context.User = principal;
                }
            }
        }
    }
}