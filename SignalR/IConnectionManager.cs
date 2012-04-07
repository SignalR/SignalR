using SignalR.Hubs;

namespace SignalR
{
    public interface IConnectionManager
    {
        dynamic GetClients<T>() where T : IHub;
        dynamic GetClients(string hubName);
        IConnection GetConnection<T>() where T : PersistentConnection;
    }
}
