using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class MemoryClient : DefaultHttpClient
    {
        private HttpMessageHandler _handler;

        public MemoryClient(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        protected override HttpMessageHandler CreateHandler()
        {
            return _handler;
        }
    }
}
