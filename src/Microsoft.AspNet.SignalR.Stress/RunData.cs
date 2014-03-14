// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.


namespace Microsoft.AspNet.SignalR.Stress
{
    public class RunData
    {
        // Basic
        public int SampleRate { get; set; }
        public int Duration { get; set; }
        public int Warmup { get; set; }
        public string Transport { get; set; }
        public string Host { get; set; }
        public string Payload { get; set; }
        public string Url { get; set; }
        public int Senders { get; set; }
        public int Connections { get; set; }
        public int SendDelay { get; set; }

        // Scaleout
        public string RedisServer { get; set; }
        public string RedisPassword { get; set; }
        public int RedisPort { get; set; }

        public string SqlConnectionString { get; set; }
        public int SqlTableCount { get; set; }
        public string ServiceBusConnectionString { get; set; }

        public int StreamCount { get; set; }
    }
}
