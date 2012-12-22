// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Configuration
{
    public class DefaultConfigurationManager : IConfigurationManager
    {
        public DefaultConfigurationManager()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(110);
            DisconnectTimeout = TimeSpan.FromSeconds(40);
            HeartbeatInterval = TimeSpan.FromSeconds(10);
            KeepAlive = TimeSpan.FromSeconds(15);
            DefaultMessageBufferSize = 1000;
        }

        public TimeSpan ConnectionTimeout
        {
            get;
            set;
        }

        public TimeSpan DisconnectTimeout
        {
            get;
            set;
        }

        public TimeSpan? KeepAlive
        {
            get;
            set;
        }

        public TimeSpan HeartbeatInterval
        {
            get;
            set;
        }

        public int DefaultMessageBufferSize
        {
            get;
            set;
        }
    }
}
