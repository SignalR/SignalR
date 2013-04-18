// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Messaging
{
    /// <summary>
    /// Common settings for scale-out message bus implementations.
    /// </summary>
    public class ScaleoutConfiguration
    {
        /// <summary>
        /// The number of messages to include in a batch before delivering to the scale-out message bus.
        /// Use in conjunction with the BatchTimeout property to configure message batching.
        /// Set this property to 0 to disable batching (the default).
        /// </summary>
        public uint BatchSize { get; set; }
        
        /// <summary>
        /// The amount of time to wait before delivering a batch of messages to the scale-out message bus.
        /// Use in conjunction with the BatchSize property to configure message batching.
        /// </summary>
        public TimeSpan BatchTimeout { get; set; }
    }
}
