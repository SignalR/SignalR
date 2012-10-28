// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR
{
    public class DefaultConfigurationManager : IConfigurationManager
    {
        public DefaultConfigurationManager()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(110);
            DisconnectTimeout = TimeSpan.FromSeconds(40);
            HeartBeatInterval = TimeSpan.FromSeconds(10);
            KeepAlive = TimeSpan.FromSeconds(30);
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

        public TimeSpan HeartBeatInterval
        {
            get;
            set;
        }
    }
}
