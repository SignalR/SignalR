using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class StatusCodeConnection : PersistentConnection
    {
        public override Task ProcessRequest(HostContext context)
        {
            string alterWhen = "____Never____";
            int statusCode = 200;

            if (!String.IsNullOrEmpty(context.Request.QueryString["alterWhen"]))
            {
                alterWhen = context.Request.QueryString["alterWhen"];
            }

            if (!String.IsNullOrEmpty(context.Request.QueryString["statusCode"]))
            {
                statusCode = Int32.Parse(context.Request.QueryString["statusCode"]);
            }

            var owinRequest = new OwinRequest(context.Environment);

            if (owinRequest.Path.Value.Contains("/" + alterWhen))
            {
                var response = new OwinResponse(context.Environment);

                // Alter status code
                response.StatusCode = statusCode;

                using (var sw = new StreamWriter(response.Body))
                {
                    sw.WriteLine("Hello world");
                    sw.Flush();
                }

                return TaskAsyncHelper.Empty;
            }

            return base.ProcessRequest(context);
        }
    }
}
