using SignalR.Abstractions;

namespace SignalR.Transports
{
    public interface ITransportManager
    {
        ITransport GetTransport(HostContext hostContext);
    }
}
