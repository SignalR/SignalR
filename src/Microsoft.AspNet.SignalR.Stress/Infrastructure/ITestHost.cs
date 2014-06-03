using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using System.IO;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public interface ITestHost : IDisposable
    {
        IDependencyResolver Resolver { get; set; }

        string Url { get; }

        void Initialize(int? keepAlive = -1,
                int? connectionTimeout = 110,
                int? disconnectTimeout = 30,
                int? transportConnectTimeout = 5,
                int? maxIncomingWebSocketMessageSize = 64 * 1024, // Default 64 KB
                bool enableAutoRejoiningGroups = false);

        Task<IResponse> Get(string uri);

        Task<IResponse> Post(string uri, IDictionary<string, string> data);

        Func<IClientTransport> TransportFactory { get; set; }
    }
}
