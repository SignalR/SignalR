using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public interface ITestHost : IDisposable
    {
        string Url { get; }

        IClientTransport Transport { get; set; }

        TextWriter ClientTraceOutput { get; set; }

        IList<IDisposable> Disposables { get; }

        IDictionary<string, string> ExtraData { get; }

        Func<IClientTransport> TransportFactory { get; set; }

        void Initialize(int? keepAlive = -1,
                        int? connectionTimeout = 110,
                        int? disconnectTimeout = 30,
                        bool enableAutoRejoiningGroups = false);

        void Shutdown();
    }
}
