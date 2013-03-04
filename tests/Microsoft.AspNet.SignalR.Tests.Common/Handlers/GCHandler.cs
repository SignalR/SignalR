using System;
using System.Web;
using System.Web.Routing;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class GCHandler : IRouteHandler, IHttpHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return this;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
