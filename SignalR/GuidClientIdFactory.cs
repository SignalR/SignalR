using System;
using System.Web;

namespace SignalR
{
    public class GuidClientIdFactory : IClientIdFactory
    {
        public string CreateClientId(HttpContextBase context)
        {
            return Guid.NewGuid().ToString("d");
        }
    }
}
