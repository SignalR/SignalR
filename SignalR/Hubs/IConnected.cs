using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Hosting;

namespace SignalR.Hubs
{
    public interface IConnected
    {
        Task Connect();
        Task Reconnect(IEnumerable<string> groups);
    }
}
