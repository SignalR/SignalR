using System.Diagnostics;
using System.Web;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class PreSendRequestHeadersModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += (s, e) => { };
        }

        public void Dispose()
        {
        }
    }
}
