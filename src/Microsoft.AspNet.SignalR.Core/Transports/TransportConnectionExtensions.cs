// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Transports
{
    internal static class TransportConnectionExtensions
    {
        internal static Task Close(this ITransportConnection connection, string connectionId)
        {
            var command = new Command
            {
                CommandType = CommandType.Disconnect
            };

            return connection.Send(new ConnectionMessage(connectionId, command));
        }

        internal static Task Abort(this ITransportConnection connection, string connectionId)
        {
            var command = new Command
            {
                CommandType = CommandType.Abort
            };

            return connection.Send(new ConnectionMessage(connectionId, command));
        }
    }
}
