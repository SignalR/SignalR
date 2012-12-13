// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Encapsulates all information about an individual SignalR connection for an <see cref="IHub"/>.
    /// </summary>
    public class HubConnectionContext : IHubConnectionContext
    {
        private readonly string _hubName;
        private readonly string _connectionId;
        private readonly Func<string, ClientHubInvocation, IEnumerable<string>, Task> _send;

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnectionContext"/>.
        /// </summary>
        public HubConnectionContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnectionContext"/>.
        /// </summary>
        /// <param name="pipelineInvoker">The pipeline invoker.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="hubName">The hub name.</param>
        /// <param name="connectionId">The connection id.</param>
        /// <param name="tracker">The connection hub state.</param>
        public HubConnectionContext(IHubPipelineInvoker pipelineInvoker, IConnection connection, string hubName, string connectionId, StateChangeTracker tracker)
        {
            _send = (signal, invocation, exclude) => pipelineInvoker.Send(new HubOutgoingInvokerContext(connection, signal, invocation, exclude));
            _connectionId = connectionId;
            _hubName = hubName;

            Caller = new StatefulSignalProxy(_send, connectionId, hubName, tracker);
            All = AllExcept();
            Others = AllExcept(connectionId);
        }

        /// <summary>
        /// All connected clients.
        /// </summary>
        public dynamic All { get; set; }

        /// <summary>
        /// All connected clients except the calling client.
        /// </summary>
        public dynamic Others { get; set; }

        /// <summary>
        /// Represents the calling client.
        /// </summary>
        public dynamic Caller { get; set; }

        /// <summary>
        /// Returns a dynamic representation of all clients except the calling client ones specified.
        /// </summary>
        /// <param name="exclude">A list of connection ids to exclude.</param>
        /// <returns>A dynamic representation of all clients except the calling client ones specified.</returns>
        public dynamic AllExcept(params string[] exclude)
        {
            // REVIEW: Should this method be params array?
            return new ClientProxy(_send, _hubName, exclude);
        }

        /// <summary>
        /// Returns a dynamic representation of all clients in a group except the calling client.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A dynamic representation of all clients in a group except the calling client.</returns>
        public dynamic OthersInGroup(string groupName)
        {
            return Group(groupName, _connectionId);
        }

        /// <summary>
        /// Returns a dynamic representation of the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="exclude">A list of connection ids to exclude.</param>
        /// <returns>A dynamic representation of the specified group.</returns>
        public dynamic Group(string groupName, params string[] exclude)
        {
            return new SignalProxy(_send, groupName, _hubName, exclude);
        }

        /// <summary>
        /// Returns a dynamic representation of the connection with the specified connectionid.
        /// </summary>
        /// <param name="connectionId">The connection id</param>
        /// <returns>A dynamic representation of the specified client.</returns>
        public dynamic Client(string connectionId)
        {
            return new SignalProxy(_send, connectionId, _hubName);
        }
    }
}
