// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.StressServer.Connections;
using Microsoft.AspNet.SignalR.Transports;

namespace Microsoft.AspNet.SignalR.Stress
{
    [Export("ConnectionRun", typeof(IRun))]
    public class ConnectionRun : RunBase
    {
        private readonly IPersistentConnectionContext _context;
        private readonly ITransportConnection _transportConnection;

        [ImportingConstructor]
        public ConnectionRun(RunData runData)
            : base(runData)
        {
            var connectionManager = new ConnectionManager(Resolver);
            _context = connectionManager.GetConnectionContext<StressConnection>();
            _transportConnection = (ITransportConnection)_context.Connection;
        }

        protected override Task Send(int senderIndex, string source)
        {
            return _context.Connection.Broadcast(Payload);
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            return _transportConnection.Receive(messageId: null,
                                                callback: (_, __) => TaskAsyncHelper.True,
                                                maxMessages: 10,
                                                state: null);
        }
    }
}
