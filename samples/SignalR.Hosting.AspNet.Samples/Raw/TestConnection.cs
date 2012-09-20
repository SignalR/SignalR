using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hosting.AspNet.Samples
{
    public class TestConnection : PersistentConnection
    {
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            return Connection.Send(connectionId, data);
        }

        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return groups;
        }
    }
}