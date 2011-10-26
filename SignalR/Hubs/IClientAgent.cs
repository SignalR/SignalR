using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public interface IClientAgent
    {
        Task Invoke(string method, params object[] args);
    }
}
