using SignalR.Hubs;

namespace SignalR
{
    public interface IConnectionManager
    {
        dynamic GetClients<T>() where T : IHub;
        IConnection GetConnection<T>() where T : PersistentConnection;
    }
}
