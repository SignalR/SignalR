using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SignalR.Infrastructure
{
    public static class PerformanceCounters
    {
        public const string CategoryName = "SignalR";

        // Connections count
        public const string ConnectionsCurrent = "Connections Current";
        public const string ConnectionsConnected = "Connections Connected";
        public const string ConnectionsDisconnected = "Connections Disconnected";

        // Connections throughput
        public const string ConnectionMessagesReceived = "Connection Messages Received";
        public const string ConnectionMessagesSent = "Connection Messages Sent";
        public const string ConnectionMessagesReceivedPerSec = "Connection Messages Received/Sec";
        public const string ConnectionMessagesSentPerSecond = "Connection Messages Sent/Second";

        // Message bus throughput
        public const string MessageBusMessagesPublishedTotal = "Messages Bus Messages Published Total";
        public const string MessageBusMessagesPublishedPerSec = "Messages Bus Messages Published/Sec";
        public const string MessageBusSubscribersCurrent = "Message Bus Subscribers Current";
        public const string MessageBusSubscribersTotal = "Message Bus Subscribers Total";
        public const string MessageBusSubscribersPerSec = "Message Bus Subscribers/Sec";

        internal static IEnumerable<CounterCreationData> Counters
        {
            get;
            private set;
        }

        static PerformanceCounters()
        {
            var counters = new List<CounterCreationData>();
            counters.Add(new CounterCreationData(ConnectionsCurrent, "The number of connections currently connected.", PerformanceCounterType.NumberOfItems32));
            counters.Add(new CounterCreationData(ConnectionsConnected, "The total number of connection Connect events since the application was started.", PerformanceCounterType.NumberOfItems32));
            counters.Add(new CounterCreationData(ConnectionsDisconnected, "The total number of connection Disconnect events since the application was started.", PerformanceCounterType.NumberOfItems32));

            counters.Add(new CounterCreationData(ConnectionMessagesReceived, "", PerformanceCounterType.NumberOfItems64));
            counters.Add(new CounterCreationData(ConnectionMessagesSent, "", PerformanceCounterType.NumberOfItems64));
            counters.Add(new CounterCreationData(ConnectionMessagesReceivedPerSec, "", PerformanceCounterType.RateOfCountsPerSecond32));
            counters.Add(new CounterCreationData(ConnectionMessagesSentPerSecond, "", PerformanceCounterType.RateOfCountsPerSecond32));

            counters.Add(new CounterCreationData(MessageBusMessagesPublishedTotal, "", PerformanceCounterType.NumberOfItems64));
            counters.Add(new CounterCreationData(MessageBusMessagesPublishedPerSec, "", PerformanceCounterType.RateOfCountsPerSecond32));
            counters.Add(new CounterCreationData(MessageBusSubscribersCurrent, "", PerformanceCounterType.NumberOfItems32));
            counters.Add(new CounterCreationData(MessageBusSubscribersTotal, "", PerformanceCounterType.NumberOfItems64));
            counters.Add(new CounterCreationData(MessageBusSubscribersPerSec, "", PerformanceCounterType.RateOfCountsPerSecond32));

            Counters = counters.ToArray();
        }
    }
}
