using SignalR.Hosting;

namespace SignalR
{
    public interface IConnectionIdFactory
    {
        string CreateConnectionId(IRequest request);
    }
}
