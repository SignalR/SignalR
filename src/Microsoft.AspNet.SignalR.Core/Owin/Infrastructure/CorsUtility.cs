using System;
using System.Collections.Generic;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Infrastructure
{
    internal static class CorsUtility
    {
        internal static void AddHeaders(OwinRequest request, OwinResponse response)
        {
            string origin = request.GetHeader("Origin");

            // Add CORS response headers support
            if (!String.IsNullOrEmpty(origin))
            {
                response.SetHeader("Access-Control-Allow-Origin", origin);
                response.SetHeader("Access-Control-Allow-Credentials", "true");
            }
        }

        internal static bool IsCrossDomainRequest(OwinRequest request)
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
