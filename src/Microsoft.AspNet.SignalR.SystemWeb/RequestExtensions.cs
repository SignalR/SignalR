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
            // Try to grab the HttpContextBase from the environment
            return request.GetOwinVariable<HttpContextBase>(typeof(HttpContextBase).FullName);
        }
    }
}
