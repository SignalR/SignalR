using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class ExamineReconnectPath : PersistentConnection
    {
        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            return Connection.Send(connectionId, request.Url.AbsolutePath.EndsWith("/reconnect"));
        }
    }
}
