// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("ClientServerMemory", typeof(IRun))]
    public class ClientServerMemoryRun : SendReceiveRun
    {
        private readonly Client.Connection[] _connections;

        [ImportingConstructor]
        public ClientServerMemoryRun(RunData runData)
            : base(runData)
        {
            _connections = new Connection[runData.Connections];
            for (int i = 0; i < runData.Connections; i++)
            {
                _connections[i] = new Connection("http://memoryhost/echo");
            }
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            Connection connection = _connections[connectionIndex];
            connection.Start(Host.Transport).Wait();

            return Microsoft.AspNet.SignalR.Infrastructure.DisposableAction.Empty;
        }

        protected override Task Send(int senderIndex, string source)
        {
            return _connections[senderIndex].Send(Payload);
        }
    }
}
