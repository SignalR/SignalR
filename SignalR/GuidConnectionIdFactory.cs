using System;
using SignalR.Hosting;
using System.Security.Principal;

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
