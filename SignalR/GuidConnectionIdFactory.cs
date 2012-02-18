using System;
using SignalR.Hosting;

namespace SignalR
{
    public class GuidConnectionIdFactory : IConnectionIdFactory
    {
        public string CreateConnectionId(IRequest request)
        {
            return Guid.NewGuid().ToString("d");
        }
    }
}
