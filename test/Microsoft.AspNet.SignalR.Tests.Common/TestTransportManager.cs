using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Transports;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class TestTransportManager : ITransportManager
    {
        public static readonly string TestTransportName = "test";
        private Dictionary<string, Func<ITransport>> _transports = new Dictionary<string, Func<ITransport>>();

        public TestTransport TestTransport { get; } = new TestTransport();

        public TestTransportManager()
        {
            _transports.Add(TestTransportName, () => TestTransport);
        }

        public void AddTransport(string name, Func<ITransport> factory)
        {
            _transports[name] = factory;
        }

        public ITransport GetTransport(HostContext hostContext)
        {
            if (_transports.TryGetValue(hostContext.Request.QueryString["transport"], out var factory))
            {
                return factory();
            }
            return null;
        }

        public bool SupportsTransport(string transportName)
        {
            return _transports.ContainsKey(transportName);
        }
    }
}
