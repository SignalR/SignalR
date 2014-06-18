// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.Crank
{
    internal class PerformanceCounters
    {
        private PerformanceCounter ServerAvailableMBytesCounter;
        private PerformanceCounter ServerTcpConnectionsEstCounter;

        public PerformanceCounters(string host, string signalRInstance)
        {
            ServerAvailableMBytesCounter = LoadCounter("Memory", "Available MBytes", host);
            ServerTcpConnectionsEstCounter = LoadCounter("TCPv4", "Connections Established", host);
        }

        public int ServerAvailableMBytes
        {
            get
            {
                return (ServerAvailableMBytesCounter == null) ? 0 : GetIntValue(ServerAvailableMBytesCounter);
            }
        }

        public int ServerTcpConnectionsEst
        {
            get
            {
                return (ServerTcpConnectionsEstCounter == null) ? 0 : GetIntValue(ServerTcpConnectionsEstCounter);
            }
        }

        private static int GetIntValue(PerformanceCounter counter)
        {
            return (int)Math.Round(counter.NextValue());
        }

        private static PerformanceCounter LoadCounter(string category, string name, string host, string instance = null)
        {
            try
            {
                var counter = new PerformanceCounter(category, name, instance, host);
                counter.NextSample();
                return counter;
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to load counter '{0}\\{1}' on host '{2}'", category, name, host);
                return null;
            }
        }
    }
}
