// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Settings for the Service Bus scale-out message bus implementation.
    /// </summary>
    public class ServiceBusScaleoutConfiguration : ScaleoutConfiguration
    {
        private int _topicCount;

        public ServiceBusScaleoutConfiguration(string connectionString, string topicPrefix)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            if (String.IsNullOrEmpty(topicPrefix))
            {
                throw new ArgumentNullException("topicPrefix");
            }

            IdleSubscriptionTimeout = TimeSpan.FromHours(1);
            ConnectionString = connectionString;
            TopicPrefix = topicPrefix;
            TopicCount = 1;
            TimeToLive = TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// The Service Bus connection string to use.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The topic prefix to use. Typically represents the app name.
        /// This must be consistent between all nodes in the web farm.
        /// </summary>
        public string TopicPrefix { get; private set; }

        /// <summary>
        /// The number of topics to send messages over. Using more topics reduces contention and may increase throughput.
        /// This must be consistent between all nodes in the web farm.
        /// Defaults to 1.
        /// </summary>
        public int TopicCount
        {
            get
            {
                return _topicCount;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _topicCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the message’s time to live value. This is the duration after
        /// which the message expires, starting from when the message is sent to the
        /// Service Bus. Messages older than their TimeToLive value will expire and no
        /// longer be retained in the message store. Subscribers will be unable to receive
        /// expired messages.
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// Specifies the time duration after which an idle subscription is deleted
        /// </summary>
        public TimeSpan IdleSubscriptionTimeout { get; set; }
    }
}