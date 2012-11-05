// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Used to indicate a Noop method operation.  Noop methods can then be handled appropriately by
    /// pipeline modules.
    /// </summary>
    public class NoopMethodDescriptor : MethodDescriptor
    {
        /// <summary>
        /// The exception that caused the noop
        /// </summary>
        public virtual Exception NoopException { get; set; }
    }
}

