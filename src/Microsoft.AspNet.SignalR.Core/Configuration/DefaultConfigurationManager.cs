﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Configuration
{
    public class DefaultConfigurationManager : IConfigurationManager
    {
        private int _keepAlive;

        public DefaultConfigurationManager()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(110);
            DisconnectTimeout = TimeSpan.FromSeconds(40);
            HeartbeatInterval = TimeSpan.FromSeconds(10);
            KeepAlive = 2;
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
        
        public int KeepAlive
        {
            get
            {
                return _keepAlive;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(Resources.Error_KeepAliveMustBeGreaterThanZero);
                }

                _keepAlive = value;
            }
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

        public bool DisableJavaScriptProxies
        {
            get;
            set;
        }
    }
}
