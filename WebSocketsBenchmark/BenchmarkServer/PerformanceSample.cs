using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BenchmarkServer
{
    public class PerformanceSample : EventArgs
    {
        public long SampleTime { get; set; }

        public long ClientsConnected { get; set; }

        public long ClientConnectionsPerSecond { get; set; }

        public long MessagesSent { get; set; }

        public long MessagesPerSecond { get; set; }

        public long LastBroadcastDuration { get; set; }

        public long BroadcastRate { get; set; }

    }
}