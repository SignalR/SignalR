// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
