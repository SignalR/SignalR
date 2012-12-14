using System;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public interface ITestHost : IDisposable
    {
        string Url { get; }

        IClientTransport Transport { get; set; }

        Func<IClientTransport> TransportFactory { get; set; }

        void Initialize(int? keepAlive = 15,
                        int? connectionTimeout = 120,
                        int? disconnectTimeout = 40,
                        int? hearbeatInterval = 10,
                        bool enableAutoRejoiningGroups = false);

        void Shutdown();
    }
}
