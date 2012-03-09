using System.Security.Principal;
using SignalR.Hosting;

namespace SignalR
{
    public interface IConnectionIdFactory
    {
        string CreateConnectionId(IRequest request, IPrincipal user);
    }
}
