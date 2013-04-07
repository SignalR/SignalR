// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    public interface INetworkObserver
    {
        Action OnCancel { get; set; }
        Action OnClose { get; set; }
        Action<ArraySegment<byte>> OnWrite { get; set; }
    }
}
