﻿using System;

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
    }
}
