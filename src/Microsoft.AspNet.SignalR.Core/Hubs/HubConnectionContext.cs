// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Encapsulates all information about an individual SignalR connection for an <see cref="IHub"/>.
    /// </summary>
    public class HubConnectionContext : HubConnectionContextBase, IHubCallerConnectionContext<object>
    {
        private readonly string _connectionId;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnectionContext"/>.
        /// </summary>
        public HubConnectionContext()
        {
            All = new NullClientProxy();
            Others = new NullClientProxy();
            Caller = new NullClientProxy();
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
            : base(connection, pipelineInvoker, hubName)
        {
            _connectionId = connectionId;

            Caller = new StatefulSignalProxy(connection, pipelineInvoker, connectionId, PrefixHelper.HubConnectionIdPrefix, hubName, tracker);
            CallerState = new CallerStateProxy(tracker);
            All = AllExcept();
            Others = AllExcept(connectionId);
        }

        /// <summary>
        /// All connected clients except the calling client.
        /// </summary>
        public dynamic Others { get; set; }

        /// <summary>
        /// Represents the calling client.
        /// </summary>
        public dynamic Caller { get; set; }

        /// <summary>
        /// Represents the calling client's state. This should be used when the state is innaccessible
        /// via the <see cref="HubConnectionContext.Caller"/> property (such as in VB.NET or in typed Hubs).
        /// </summary>
        public dynamic CallerState { get; set; }

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
        /// Returns a dynamic representation of all clients in the specified groups except the calling client.
        /// </summary>
        /// <param name="groupNames">The name of the groups</param>
        /// <returns>A dynamic representation of all clients in a group except the calling client.</returns>
        public dynamic OthersInGroups(IList<string> groupNames)
        {
            return Groups(groupNames, _connectionId);
        }
    }
}
