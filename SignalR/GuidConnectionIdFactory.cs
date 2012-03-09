using System;
using System.Security.Principal;
using SignalR.Hosting;

namespace SignalR
{
    public class GuidConnectionIdFactory : IConnectionIdFactory
    {
        public string CreateConnectionId(IRequest request, IPrincipal user)
        {
            return Guid.NewGuid().ToString("d");
        }
    }
}
