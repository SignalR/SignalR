using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting.Memory;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class MemoryTestHost : TracingTestHost
    {
        private readonly MemoryHost _host;

        public MemoryTestHost(MemoryHost host, string logPath)
            : base(logPath)
        {
            _host = host;
        }

        public override string Url
        {
            get
            {
                return "http://memoryhost";
            }
        }

        public override void Initialize(int? keepAlive = -1,
                                        int? connectionTimeout = 110,
                                        int? disconnectTimeout = 30,
                                        int? transportConnectTimeout = 5,
                                        int? maxIncomingWebSocketMessageSize = 64 * 1024,
                                        bool enableAutoRejoiningGroups = false,
                                        MessageBusType messageBusType = MessageBusType.Default)
        {
            base.Initialize(keepAlive, 
                            connectionTimeout,
                            disconnectTimeout,
                            transportConnectTimeout,
                            maxIncomingWebSocketMessageSize,
                            enableAutoRejoiningGroups,
                            messageBusType);

            _host.Configure(app =>
            {
                Initializer.ConfigureRoutes(app, Resolver);
            });
        }

        public override Task Get(string uri)
        {
            return _host.Get(uri, r => { }, isLongRunning: false);
        }

        public override Task Post(string uri, IDictionary<string, string> data)
        {
            return _host.Post(uri, r => { }, data, isLongRunning: false);
        }

        public override void Dispose()
        {
            _host.Dispose();

            base.Dispose();
        }
    }
}
