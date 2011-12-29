using SignalR.Abstractions;

namespace SignalR
{
    public interface IConnectionIdFactory
    {
        string CreateConnectionId(IRequest request);
    }
}
