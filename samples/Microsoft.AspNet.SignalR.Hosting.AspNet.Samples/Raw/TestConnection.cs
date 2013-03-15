using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples
{
    public class TestConnection : PersistentConnection
    {
        public override Task ProcessRequest(Hosting.HostContext context)
        {
           var ct = context.Request.Headers.GetValues("Content-Type");
           return base.ProcessRequest(context);
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            return Connection.Send(connectionId, data);
        }
    }
}