using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Hosting;

namespace SignalR.Hubs
{
    public interface IConnected
    {
        Task Connect(IRequest request, IEnumerable<string> groups);
        Task Reconnect(IRequest request, IEnumerable<string> groups);
    }
}
