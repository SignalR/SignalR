using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public interface IDisconnect
    {
        Task Disconnect();
    }
}
