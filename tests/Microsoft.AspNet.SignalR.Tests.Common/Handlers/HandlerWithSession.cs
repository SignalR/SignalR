using System;
using System.Web;
using System.Web.Routing;
using System.Web.SessionState;

namespace Microsoft.AspNet.SignalR.Tests.Common.Handlers
{
    public class HandlerWithSession : IRouteHandler, IHttpHandler, IRequiresSessionState
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return this;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Write("Hello with session");
        }
    }
}
