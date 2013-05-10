using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public class AllowCrossOriginMiddleware : PathMatchingMiddleware
    {
        public AllowCrossOriginMiddleware(OwinMiddleware next, string path)
            : base(next, path)
        {
        }

        protected override Task ProcessRequest(OwinRequest request, OwinResponse response)
        {
            string origin = request.GetHeader("Origin");

            // Add CORS response headers support
            if (!String.IsNullOrEmpty(origin))
            {
                response.SetHeader("Access-Control-Allow-Origin", origin);
                response.SetHeader("Access-Control-Allow-Credentials", "true");
            }

            return Next.Invoke(request, response);
        }
    }
}
