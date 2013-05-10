using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public class DenyCrossOriginMiddleware : PathMatchingMiddleware
    {
        public DenyCrossOriginMiddleware(OwinMiddleware next, string path)
            : base(next, path)
        {
        }

        protected override Task ProcessRequest(OwinRequest request, OwinResponse response)
        {
            string origin = request.GetHeader("Origin");

            string callback = null;

            IDictionary<string, string[]> query = request.GetQuery();
            string[] callbacks;

            if (query.TryGetValue("callback", out callbacks) &&
                callbacks.Length > 0)
            {
                callback = callbacks[0];
            }

            // If it's a JSONP request and we're not allowing cross domain requests then block it
            // If there's an origin header and it's not a same origin request then block it.

            if (!String.IsNullOrEmpty(callback) ||
                (!String.IsNullOrEmpty(origin) && !IsSameOrigin(request.Uri, origin)))
            {
                response.StatusCode = 403;

                // REVIEW: Because MS.Owin is lacking
                response.Environment[OwinConstants.ResponseReasonPhrase] = Resources.Forbidden_CrossDomainIsDisabled;

                return TaskAsyncHelper.Empty;
            }

            return Next.Invoke(request, response);
        }

        private static bool IsSameOrigin(Uri requestUri, string origin)
        {
            Uri originUri;
            if (!Uri.TryCreate(origin.Trim(), UriKind.Absolute, out originUri))
            {
                return false;
            }

            return (requestUri.Scheme == originUri.Scheme) &&
                   (requestUri.Host == originUri.Host) &&
                   (requestUri.Port == originUri.Port);
        }
    }
}
