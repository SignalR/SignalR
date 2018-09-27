// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class HubConnectionContextBase : IHubConnectionContext<object>
    {
        private readonly ITraceManager _traceManager;

        public HubConnectionContextBase()
        {
        }

        public HubConnectionContextBase(IConnection connection, IHubPipelineInvoker invoker, string hubName) : this(connection, invoker, hubName, TraceManager.Default)
        {
        }

        public HubConnectionContextBase(IConnection connection, IHubPipelineInvoker invoker, string hubName, ITraceManager traceManager)
        {
            Connection = connection;
            Invoker = invoker;
            HubName = hubName;
            _traceManager = traceManager;

            All = AllExcept();
        }

        protected IHubPipelineInvoker Invoker { get; private set; }
        protected IConnection Connection { get; private set; }
        protected string HubName { get; private set; }

        public dynamic All
        {
            get;
            set;
        }

        /// <summary>
        /// Returns a dynamic representation of all clients except the calling client ones specified.
        /// </summary>
        /// <param name="excludeConnectionIds">The list of connection ids to exclude</param>
        /// <returns>A dynamic representation of all clients except the calling client ones specified.</returns>
        public dynamic AllExcept(params string[] excludeConnectionIds)
        {
            return new ClientProxy(Connection, Invoker, HubName, PrefixHelper.GetPrefixedConnectionIds(excludeConnectionIds), _traceManager);
        }

        /// <summary>
        /// Returns a dynamic representation of the connection with the specified connectionid.
        /// </summary>
        /// <param name="connectionId">The connection id</param>
        /// <returns>A dynamic representation of the specified client.</returns>
        public dynamic Client(string connectionId)
        {
            if (String.IsNullOrEmpty(connectionId))
            {
                throw new ArgumentException(Resources.Error_ArgumentNullOrEmpty, "connectionId");
            }

            return new ConnectionIdProxy(Connection,
                                         Invoker,
                                         connectionId,
                                         HubName,
                                         _traceManager);
        }

        /// <summary>
        /// Returns a dynamic representation of the connections with the specified connectionids.
        /// </summary>
        /// <param name="connectionIds">The connection ids.</param>
        /// <returns>A dynamic representation of the specified clients.</returns>
        public dynamic Clients(IList<string> connectionIds)
        {
            if (connectionIds == null)
            {
                throw new ArgumentNullException("connectionIds");
            }

            return new MultipleSignalProxy(Connection,
                                           Invoker,
                                           connectionIds,
                                           HubName,
                                           PrefixHelper.HubConnectionIdPrefix,
                                           ListHelper<string>.Empty,
                                           _traceManager);
        }

        /// <summary>
        /// Returns a dynamic representation of the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="excludeConnectionIds">The list of connection ids to exclude</param>
        /// <returns>A dynamic representation of the specified group.</returns>
        public dynamic Group(string groupName, params string[] excludeConnectionIds)
        {
            if (String.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException(Resources.Error_ArgumentNullOrEmpty, "groupName");
            }

            return new GroupProxy(Connection,
                                  Invoker,
                                  groupName,
                                  HubName,
                                  PrefixHelper.GetPrefixedConnectionIds(excludeConnectionIds),
                                  _traceManager);
        }

        /// <summary>
        /// Returns a dynamic representation of the specified groups.
        /// </summary>
        /// <param name="groupNames">The names of the groups.</param>
        /// <param name="excludeConnectionIds">The list of connection ids to exclude</param>
        /// <returns>A dynamic representation of the specified groups.</returns>
        public dynamic Groups(IList<string> groupNames, params string[] excludeConnectionIds)
        {
            if (groupNames == null)
            {
                throw new ArgumentNullException("groupNames");
            }

            return new MultipleSignalProxy(Connection,
                                           Invoker,
                                           groupNames,
                                           HubName,
                                           PrefixHelper.HubGroupPrefix,
                                           PrefixHelper.GetPrefixedConnectionIds(excludeConnectionIds),
                                           _traceManager);
        }

        public dynamic User(string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException("userId");
            }

            return new UserProxy(Connection, Invoker, userId, HubName, _traceManager);
        }

        public dynamic Users(IList<string> userIds)
        {
            if (userIds == null)
            {
                throw new ArgumentNullException("userIds");
            }

            return new MultipleSignalProxy(Connection,
                                           Invoker,
                                           userIds,
                                           HubName,
                                           PrefixHelper.HubUserPrefix,
                                           ListHelper<string>.Empty,
                                           _traceManager);
        }
    }
}
