using System;
using System.Collections.Generic;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Infrastructure
{
    internal static class CorsUtility
    {
        internal static void AddHeaders(IOwinContext context)
        {
            string origin = context.Request.Headers.Get("Origin");

            // Add CORS response headers support
            if (!String.IsNullOrEmpty(origin))
            {
                context.Response.Headers.Set("Access-Control-Allow-Origin", origin);
                context.Response.Headers.Set("Access-Control-Allow-Credentials", "true");
            }
        }

        internal static bool IsCrossDomainRequest(IOwinRequest request)
        {
            string origin = request.Headers.Get("Origin");
            string callback = request.Query.Get("callback");

            // If it's a JSONP request and we're not allowing cross domain requests then block it
            // If there's an origin header and it's not a same origin request then block it.

            if (!String.IsNullOrEmpty(callback) ||
                (!String.IsNullOrEmpty(origin) && !IsSameOrigin(request.Uri, origin)))
            {
                return true;
            }

            return false;
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
