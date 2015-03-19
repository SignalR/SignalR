// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Provides methods that communicate with SignalR connections that connected to a <see cref="Hub"/>.
    /// </summary>
    public abstract class Hub : HubBase
    {
        public IHubCallerConnectionContext<dynamic> Clients
        {
            get
            {
                return HubClients;
            }
            set
            {
                HubClients = value;
            }
        }
    }
}
