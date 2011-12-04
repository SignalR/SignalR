using System;
using System.Web;

namespace SignalR
{
    public class GuidConnectionIdFactory : IConnectionIdFactory
    {
        public string CreateConnectionId(HttpContextBase context)
        {
            return Guid.NewGuid().ToString("d");
        }
    }
}
