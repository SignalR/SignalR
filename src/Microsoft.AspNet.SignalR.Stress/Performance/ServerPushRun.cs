// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("ServerPush", typeof(IRun))]
    public class ServerPushRun : HostedRun
    {
        private HubConnection[] _connections;
        private IHubProxy[] _proxies;
        private volatile int _receives;

        [ImportingConstructor]
        public ServerPushRun(RunData runData) : base(runData)
        {
            _connections = new HubConnection[Connections];
            _proxies = new HubProxy[Connections];
        }

        public override void Initialize()
        {
            for (int i = 0; i < Connections; i++)
            {
                _connections[i] = new HubConnection(RunData.Url);
            }

            base.Initialize();
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            // set up the client and start it
            HubConnection connection = _connections[connectionIndex];
            IHubProxy proxy = connection.CreateHubProxy("ServerPushHub");

            proxy.On<object>("send", _=> _receives++);

            connection.Start(Host.TransportFactory()).Wait();

            _proxies[connectionIndex] = proxy;

            return Microsoft.AspNet.SignalR.Infrastructure.DisposableAction.Empty;
        }

        public override void RunTest()
        {
        }

        public override void Sample()
        {
            _proxies[0].Invoke("Start");
        }

        public override void Record()
        {
            _proxies[0].Invoke("Stop");
            Console.WriteLine("Receives: " + _receives);
        }

        protected override Task Send(int senderIndex, string source)
        {
            throw new NotImplementedException();
        }
    }
}
