using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class MemoryTestHost : ITestHost
    {
        private readonly MemoryHost _host;
        
        public MemoryTestHost(MemoryHost host)
        {
            _host = host;
        }

        public string Url
        {
            get
            {
                return "http://foo";
            }
        }

        public IClientTransport Transport { get; set; }

        public void Initialize()
        {
            _host.MapHubs();
        }

        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
