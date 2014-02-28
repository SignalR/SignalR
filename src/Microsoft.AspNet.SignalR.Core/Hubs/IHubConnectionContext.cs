﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Encapsulates all information about a SignalR connection for an <see cref="IHub"/>.
    /// </summary>
    public interface IHubConnectionContext
    {
        dynamic All { get; }
        dynamic AllExcept(params string[] excludeConnectionIds);
        
        dynamic Client(string connectionId);
        dynamic Clients(IList<string> connectionIds);

        dynamic Group(string groupName, params string[] excludeConnectionIds);
        dynamic Groups(IList<string> groupNames, params string[] excludeConnectionIds);

        dynamic User(string userId);
    }
}
