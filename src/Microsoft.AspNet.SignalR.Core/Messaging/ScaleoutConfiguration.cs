// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Messaging
{
    /// <summary>
    /// Common settings for scale-out message bus implementations.
    /// </summary>
    public class ScaleoutConfiguration
    {
        private int _maxQueueLength;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ScaleoutConfiguration()
        {
            QueueBehavior = QueuingBehavior.InitialOnly;
            _maxQueueLength = 1000;
        }

        /// <summary>
        /// Gets or sets a value that represents the queuing behavior for scale-out messages.
        /// Defaults to <see cref="Microsoft.AspNet.SignalR.QueuingBehavior.InitialOnly">QueuingBehavior.InitialOnly</see>
        /// </summary>
        public virtual QueuingBehavior QueueBehavior { get; set; }

        /// <summary>
        /// The maximum length of the outgoing send queue. Messages being sent to the backplane are queued
        /// up to this length. After the max length is reached, further sends will throw an <see cref="System.InvalidOperationException">InvalidOperationException</see>.
        /// </summary>
        public virtual int MaxQueueLength
        {
            get
            {
                return _maxQueueLength;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _maxQueueLength = value;
            }
        }
    }
}
