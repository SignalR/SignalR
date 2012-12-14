using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNet.SignalR.Owin;

namespace Microsoft.AspNet.SignalR
{
    public static class RequestExtensions
    {        
        public static HttpContextBase GetHttpContext(this IRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var env = request.Items.Get<IDictionary<string, object>>(ServerRequest.OwinEnvironmentKey);

            if (env == null)
            {
                // Owin environment not detected
                return null;
            }

            // Try to grab the HttpContextBase from the environment
            return env.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
        }

        private static T Get<T>(this IDictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) ? (T)value : default(T);
        }
    }
}
