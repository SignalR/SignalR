// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Transports
{
    internal static class TransportConnectionExtensions
    {
        internal static Task Initialize(this ITransportConnection connection, string connectionId)
        {
            return SendCommand(connection, connectionId, CommandType.Initializing);
        }

        internal static Task Abort(this ITransportConnection connection, string connectionId)
        {
            return SendCommand(connection, connectionId, CommandType.Abort);
        }

        private static Task SendCommand(ITransportConnection connection, string connectionId, CommandType commandType)
        {
            var command = new Command
            {
                CommandType = commandType
            };

            var message = new ConnectionMessage(PrefixHelper.GetConnectionId(connectionId),
                                                command);

            return connection.Send(message);
        }
    }
}
