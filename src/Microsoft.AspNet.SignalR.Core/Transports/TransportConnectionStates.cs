// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Transports
{
    [Flags]
    public enum TransportConnectionStates
    {
        None = 0,
        Added = 1,
        Removed = 2,
        Replaced = 4,
        QueueDrained = 8,
        HttpRequestEnded = 16,
        Disconnected = 32,
        Aborted = 64,
        Disposed = 65536,
    }
}
