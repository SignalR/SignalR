// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

