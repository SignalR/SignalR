// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNet.SignalR.Crank
{
    internal class ConnectionsSample
    {
        private List<int[]> states = new List<int[]>();

        public ConnectionsSample(string testPhase, TimeSpan timestamp, int serverAvailableMBytes, int serverTcpConnectionsEst)
        {
            TestPhase = testPhase;
            TimeStamp = timestamp;
            ServerAvailableMBytes = serverAvailableMBytes;
            ServerTcpConnectionsEst = serverTcpConnectionsEst;
        }

        public string TestPhase { get; set; }

        public TimeSpan TimeStamp { get; private set; }

        public int ServerAvailableMBytes { get; private set; }

        public int ServerTcpConnectionsEst { get; set; }

        public int Count
        {
            get
            {
                return states.Count;
            }
        }

        public int Connected
        {
            get
            {
                return GetState(0);
            }
        }

        public int Reconnected
        {
            get
            {
                return GetState(1);
            }
        }

        public int Disconnected
        {
            get
            {
                return GetState(2);
            }
        }

        private int GetState(int i)
        {
            lock (states)
            {
                return states.Sum(arr => arr[i]);
            }
        }

        public void Add(int[] newStates)
        {
            Debug.Assert(newStates.Length == 3);
            lock (states)
            {
                states.Add(newStates);
            }
        }
    }
}
