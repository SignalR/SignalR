// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class HubConnectionContextBase : IHubConnectionContext<object>
    {
        public HubConnectionContextBase()
        {
        }

        public HubConnectionContextBase(IConnection connection, IHubPipelineInvoker invoker, string hubName)
        {
            Connection = connection;
            Invoker = invoker;
            HubName = hubName;

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
            return new ClientProxy(Connection, Invoker, HubName, PrefixHelper.GetPrefixedConnectionIds(excludeConnectionIds));
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
                                         HubName);
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
                                           ListHelper<string>.Empty);
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
                                  PrefixHelper.GetPrefixedConnectionIds(excludeConnectionIds));
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
                                           PrefixHelper.GetPrefixedConnectionIds(excludeConnectionIds));
        }

        public dynamic User(string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException("userId");
            }

            return new UserProxy(Connection, Invoker, userId, HubName);
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
                                           ListHelper<string>.Empty);
        }
    }
}
