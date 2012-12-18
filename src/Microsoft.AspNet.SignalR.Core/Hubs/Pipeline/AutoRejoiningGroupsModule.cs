// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// By default, clients that are reconnecting to the server will be removed from all groups they may have previously been a member of.
    /// This module will allow all clients to rejoin the all of the hub groups the claim to be a member of automatically on connect and reconnect.
    /// Enabling this module may be insecure because untrusted clients may claim to be a member of groups they were never authorized to join.
    /// </summary>
    public class AutoRejoiningGroupsModule : HubPipelineModule
    {
        public override Func<HubDescriptor, IRequest, IEnumerable<string>, IEnumerable<string>> BuildRejoiningGroups(Func<HubDescriptor, IRequest, IEnumerable<string>, IEnumerable<string>> rejoiningGroups)
        {
            return (hubDescriptor, request, groups) => groups;
        }
    }
}
