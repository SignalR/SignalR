using SignalR.Hosting;

namespace SignalR.Transports
{
    public interface ITransportManager
    {
        ITransport GetTransport(HostContext hostContext);
        bool SupportsTransport(string transportName);
    }
}
