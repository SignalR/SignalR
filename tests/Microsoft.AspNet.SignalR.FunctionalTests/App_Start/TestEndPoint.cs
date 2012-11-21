using System.Web;
using System.Web.Routing;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class TestEndPoint : IRouteHandler, IHttpHandler
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
            context.Response.Write("Pong");
        }
    }
}
