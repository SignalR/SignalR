using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public interface IConnected
    {
        Task Connect(IEnumerable<string> groups);
        Task Reconnect(IEnumerable<string> groups);
    }
}
