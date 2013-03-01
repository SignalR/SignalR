// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public interface IClientTransport : IDisposable
    {
        string Name { get; }
        bool SupportsKeepAlive { get; }

        Task<NegotiationResponse> Negotiate(IConnection connection);
        Task Start(IConnection connection, string data, CancellationToken disconnectToken);
        Task Send(IConnection connection, string data);
        void Abort(IConnection connection, TimeSpan timeout);

        void LostConnection(IConnection connection);
    }
}

