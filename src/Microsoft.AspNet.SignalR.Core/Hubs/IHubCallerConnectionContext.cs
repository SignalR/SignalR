﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Encapsulates all information about an individual SignalR connection for an <see cref="IHub"/>.
    /// </summary>
    public interface IHubCallerConnectionContext<T> : IHubConnectionContext<T>
    {
        T Caller { get; }
        dynamic CallerState { get; }
        T Others { get; }
        T OthersInGroup(string groupName);
        T OthersInGroups(IList<string> groupNames);
    }
}
