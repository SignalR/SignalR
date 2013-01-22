// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public interface IHubConnection : IConnection
    {
        string RegisterCallback(Action<HubResult> callback);
        JsonSerializer JsonSerializer { get; }
    }
}
