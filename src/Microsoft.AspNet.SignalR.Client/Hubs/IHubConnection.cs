﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public interface IHubConnection : IConnection
    {
        string RegisterCallback(Action<HubResult> callback);
        void RemoveCallback(string callbackId);
    }
}
