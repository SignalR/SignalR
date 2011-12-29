using System;
using SignalR.Abstractions;

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
