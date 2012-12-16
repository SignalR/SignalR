// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR
{
    public static class ConnectionExtensions
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

        /// <summary>
        /// Sends a message to all connections subscribed to the specified signal. An example of signal may be a
        /// specific connection id, or fully qualified group name (Use <see cref="IGroupManager"/> to manipulate groups).
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="signal">The signal to send to.</param>
        /// <param name="value">The value to publish.</param>
        /// <param name="exclude">The list of connection ids to exclude</param>
        /// <returns>A task that represents when the broadcast is complete.</returns>
        public static Task Send(this IConnection connection, string signal, object value, params string[] exclude)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (signal == null)
            {
                throw new ArgumentNullException("signal");
            }

            var message = new ConnectionMessage(signal, value)
            {
                ExcludedSignals = exclude,
            };

            return connection.Send(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="connectionId"></param>
        /// <param name="value"></param>
        /// <param name="waitForReply"></param>
        /// <returns></returns>
        public static Task Send(this IConnection connection, string connectionId, object value, bool waitForReply)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (connectionId == null)
            {
                throw new ArgumentNullException("connectionId");
            }

            var message = new ConnectionMessage(connectionId, value)
            {
                WaitForReply = waitForReply
            };

            return connection.Send(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public static Task Ping(this IConnection connection, string connectionId)
        {
            var command = new Command
            {
                CommandType = CommandType.Ping
            };
            return Send(connection, connectionId, command, waitForReply: true);
        }

        /// <summary>
        /// Broadcasts a value to all connections, excluding the connection ids specified.
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="value">The value to broadcast.</param>
        /// <param name="exclude">The list of connection ids to exclude</param>
        /// <returns>A task that represents when the broadcast is complete.</returns>
        public static Task Broadcast(this IConnection connection, object value, params string[] exclude)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            return connection.Send(connection.DefaultSignal, value, exclude);
        }
    }
}
