// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// Implemented on anything that has the ability to write raw binary data
    /// </summary>
    public interface IBinaryWriter
    {
        void Write(ArraySegment<byte> data);
    }
}
