using System;
using System.Security.Principal;
using SignalR.Hosting;

namespace SignalR
{
    public class GuidConnectionIdGenerator : IConnectionIdGenerator
    {
        public string GenerateConnectionId(IRequest request, IPrincipal user)
        {
            return Guid.NewGuid().ToString("d");
        }
    }
}
