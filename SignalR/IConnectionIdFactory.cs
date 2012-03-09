using SignalR.Hosting;
using System.Security.Principal;

namespace SignalR
{
    public interface IConnectionIdFactory
    {
        string CreateConnectionId(IRequest request, IPrincipal user);
    }
}
