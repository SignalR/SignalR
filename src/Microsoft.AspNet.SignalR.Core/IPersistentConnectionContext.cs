// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Provides access to information about a <see cref="PersistentConnection" />.
    /// </summary>
    public interface IPersistentConnectionContext
    {
        /// <summary>
        /// Gets the <see cref="IConnection" /> for the <see cref="PersistentConnection" />.
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Gets the <see cref="IConnectionGroupManager" /> for the <see cref="PersistentConnection" />.
        /// </summary>
        IConnectionGroupManager Groups { get; }
    }
}
