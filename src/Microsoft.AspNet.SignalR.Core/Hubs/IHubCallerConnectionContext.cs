// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Encapsulates all information about an individual SignalR connection for an <see cref="IHub"/>.
    /// </summary>
    public interface IHubCallerConnectionContext : IHubConnectionContext
    {
        dynamic Caller { get; }
        dynamic Others { get; }
        dynamic OthersInGroup(string groupName);
        dynamic OthersInGroups(IList<string> groupNames);
    }
}
