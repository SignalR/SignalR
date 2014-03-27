// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class ExternalTestHost : ITestHost
    {
        public ExternalTestHost(string url)
        {
            Url = url;
            Disposables = new List<IDisposable>();
        }

        public string Url { get; private set; }

        public Client.Transports.IClientTransport Transport { get; set; }

        public System.IO.TextWriter ClientTraceOutput { get; set; }

        public IDependencyResolver Resolver { get; set; }

        public IList<IDisposable> Disposables
        {
            get;
            private set;
        }

        public IDictionary<string, string> ExtraData
        {
            get { throw new NotImplementedException(); }
        }

        public Func<Client.Transports.IClientTransport> TransportFactory { get; set; }

        public void Initialize(int? keepAlive = -1,
                               int? connectionTimeout = 110,                               
                               int? disconnectTimeout = 30,
                               int? transportConnectTimeout = 5,
                               int? maxIncomingWebSocketMessageSize = 64 * 1024,
                               bool enableAutoRejoiningGroups = false,
                               MessageBusType type = MessageBusType.Default)
        {
            // nothing to initialize since it is external! 
        }

        public Task Get(string uri)
        {
            throw new NotImplementedException();
        }

        public Task Post(string uri, IDictionary<string, string> data)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            // nothing to shutdown since it is external! 
        }

        public void Dispose()
        {
            // nothing to dispose since it is external! 
        }
    }
}
