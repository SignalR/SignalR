using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Redis
{
    public struct RedisEndPoint
    {
        public string IpAddress;
        public int Port;
        public string Password;
    }
}
