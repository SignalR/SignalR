// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public interface IClientTransport : IDisposable
    {
        string Name { get; }
        bool SupportsKeepAlive { get; }

        Task<NegotiationResponse> Negotiate(IConnection connection, string connectionData);
        Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken);
        Task Send(IConnection connection, string data, string connectionData);
        void Abort(IConnection connection, TimeSpan timeout, string connectionData);

        void LostConnection(IConnection connection);
    }
}

