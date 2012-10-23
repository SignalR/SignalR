using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class QueryString : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var id = context.Request.QueryString["id"];
            context.Response.ContentType = "text/plain";
            context.Response.Write("Hello World " + id);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}