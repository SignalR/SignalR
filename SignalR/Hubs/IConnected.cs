using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public interface IConnected
    {
        Task Connect();
        Task Reconnect(IEnumerable<string> groups);
    }
}
