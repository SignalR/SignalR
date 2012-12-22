// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Globalization;

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

        /// <summary>
        /// Indicates how many Heartbeats to wait before triggering keep alive.  To convert this
        /// value to a time span simply multiply it by the HeartbeatInterval.
        /// </summary>
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
    }
}
