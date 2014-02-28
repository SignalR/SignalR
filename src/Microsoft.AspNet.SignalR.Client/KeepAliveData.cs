// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    /// Class to store all the Keep Alive properties
    /// </summary>
    public class KeepAliveData
    {
        // Determines when we warn the developer that the connection may be lost
        private const double _keepAliveWarnAt = 2.0 / 3.0;

        // Timeout to designate when to force the connection into reconnecting
        public TimeSpan Timeout { get; private set; }

        // Timeout to designate when to warn the developer that the connection may be dead or is hanging.
        public TimeSpan TimeoutWarning { get; private set; }

        // Frequency with which we check the keep alive.  It must be short in order to not miss/pick up any changes
        public TimeSpan CheckInterval { get; private set; }

        public KeepAliveData(TimeSpan timeout)
        {
            Timeout = timeout;
            TimeoutWarning = TimeSpan.FromTicks((long)(Timeout.Ticks * _keepAliveWarnAt));
            CheckInterval = TimeSpan.FromTicks((Timeout.Ticks - TimeoutWarning.Ticks) / 3);
        }

        public KeepAliveData(TimeSpan timeout, TimeSpan timeoutWarning, TimeSpan checkInterval)
        {
            Timeout = timeout;
            TimeoutWarning = timeoutWarning;
            CheckInterval = checkInterval;
        }
    }
}
