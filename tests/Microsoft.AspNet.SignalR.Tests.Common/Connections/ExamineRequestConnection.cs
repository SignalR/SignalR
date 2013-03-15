using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class ExamineRequestConnection : PersistentConnection
    {
        public string NegotiateRequestType { get; private set; }
        public string PingRequestType { get; private set; }

        public override Task ProcessRequest(Hosting.HostContext context)
        {
            if (IsNegotiationRequest(context.Request))
            {
                NegotiateRequestType = context.Request.Headers.GetValues("Content-Type")[0];
            }

            else if (IsPingRequest(context.Request))
            {
                PingRequestType = context.Request.Headers.GetValues("Content-Type")[0];
            }

            return base.ProcessRequest(context);
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return Connection.Send(connectionId, NegotiateRequestType);
            // return base.OnConnected(request, connectionId);
        }

        private static bool IsNegotiationRequest(IRequest request)
        {
            return request.Url.LocalPath.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPingRequest(IRequest request)
        {
            return request.Url.LocalPath.EndsWith("/ping", StringComparison.OrdinalIgnoreCase);
        }
    }
}
