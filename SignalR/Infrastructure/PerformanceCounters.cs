using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Defines performance counters.
    /// </summary>
    public static class PerformanceCounters
    {
        public const string CategoryName = "SignalR";

        // Connections count
        public const string ConnectionsConnected = "Connections Connected";
        public const string ConnectionsReconnected = "Connections Reconnected";
        public const string ConnectionsDisconnected = "Connections Disconnected";
        public const string ConnectionsCurrent = "Connections Current";
        //public const string ConnectionsCurrentWebSockets = "Connections Current (WebSockets)";
        //public const string ConnectionsCurrentServerSentEvents = "Connections Current (Server Sent Events)";
        //public const string ConnectionsCurrentForeverFrame = "Connections Current (Forever Frame)";
        //public const string ConnectionsCurrentLongPolling = "Connections Current (Long Polling)";

        // Connections throughput
        public const string ConnectionMessagesReceivedTotal = "Connection Messages Received Total";
        public const string ConnectionMessagesSentTotal = "Connection Messages Sent Total";
        public const string ConnectionMessagesReceivedPerSec = "Connection Messages Received/Sec";
        public const string ConnectionMessagesSentPerSec = "Connection Messages Sent/Sec";

        // Message bus
        public const string MessageBusMessagesPublishedTotal = "Messages Bus Messages Published Total";
        public const string MessageBusMessagesPublishedPerSec = "Messages Bus Messages Published/Sec";
        public const string MessageBusSubscribersCurrent = "Message Bus Subscribers Current";
        public const string MessageBusSubscribersTotal = "Message Bus Subscribers Total";
        public const string MessageBusSubscribersPerSec = "Message Bus Subscribers/Sec";
        public const string MessageBusAllocatedWorkers = "Message Bus Allocated Workers";
        public const string MessageBusBusyWorkers = "Message Bus Busy Workers";

        // Errors
        public const string ErrorsAllTotal = "Errors: All Total";
        public const string ErrorsAllPerSec = "Errors: All/Sec";
        public const string ErrorsHubResolutionTotal = "Errors: Hub Resolution Total";
        public const string ErrorsHubResolutionPerSec = "Errors: Hub Resolution/Sec";
        public const string ErrorsHubInvocationTotal = "Errors: Hub Invocation Total";
        public const string ErrorsHubInvocationPerSec = "Errors: Hub Invocation/Sec";
        public const string ErrorsTransportTotal = "Errors: Tranport Total";
        public const string ErrorsTransportPerSec = "Errors: Transport/Sec";

        internal static CounterCreationData[] Counters
        {
            get;
            private set;
        }

        static PerformanceCounters()
        {
            Counters = new []
            {
                new CounterCreationData(ConnectionsConnected, "The total number of connection Connect events since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(ConnectionsReconnected, "The total number of connection Reconnect events since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(ConnectionsDisconnected, "The total number of connection Disconnect events since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(ConnectionsCurrent, "The number of connections currently connected.", PerformanceCounterType.NumberOfItems32),
                //new CounterCreationData(ConnectionsCurrentWebSockets, "The number of connections using the WebSockets transport currently connected.", PerformanceCounterType.NumberOfItems32),
                //new CounterCreationData(ConnectionsCurrentServerSentEvents, "The number of connections using the Server Sent Events transport  currently connected.", PerformanceCounterType.NumberOfItems32),
                //new CounterCreationData(ConnectionsCurrentForeverFrame, "The number of connections using the Forever Frame transport  currently connected.", PerformanceCounterType.NumberOfItems32),
                //new CounterCreationData(ConnectionsCurrentLongPolling, "The number of connections using the Long Polling transport  currently connected.", PerformanceCounterType.NumberOfItems32),

                new CounterCreationData(ConnectionMessagesReceivedTotal, "The toal number of messages received by connections (server to client) since the application was started.", PerformanceCounterType.NumberOfItems64),
                new CounterCreationData(ConnectionMessagesSentTotal, "The total number of messages sent by connections (client to server) since the application was started.", PerformanceCounterType.NumberOfItems64),
                new CounterCreationData(ConnectionMessagesReceivedPerSec, "The number of messages received by connections (server to client) per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData(ConnectionMessagesSentPerSec, "The number of messages sent by connections (client to server) per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                
                new CounterCreationData(MessageBusMessagesPublishedTotal, "The total number of messages published to the message bus since the application was started.", PerformanceCounterType.NumberOfItems64),
                new CounterCreationData(MessageBusMessagesPublishedPerSec, "The number of messages published to the message bus per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData(MessageBusSubscribersCurrent, "The current number of subscribers to the message bus.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(MessageBusSubscribersTotal, "The total number of subscribers to the message bus since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(MessageBusSubscribersPerSec, "The number of new subscribers to the message bus per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData(MessageBusAllocatedWorkers, "The number of workers allocated to deliver messages in the message bus.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(MessageBusBusyWorkers, "The number of workers currently busy delivering messages in the message bus.", PerformanceCounterType.NumberOfItems32),

                new CounterCreationData(ErrorsAllTotal, "The total number of all errors processed since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(ErrorsAllPerSec, "The number of all errors processed per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData(ErrorsHubResolutionTotal, "The total number of hub resolution errors processed since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(ErrorsHubResolutionPerSec, "The number of hub resolution errors per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData(ErrorsHubInvocationTotal, "The total number of hub invocation errors processed since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(ErrorsHubInvocationPerSec, "The number of hub invocation errors per second.", PerformanceCounterType.RateOfCountsPerSecond32),
                new CounterCreationData(ErrorsTransportTotal, "The total number of transport errors processed since the application was started.", PerformanceCounterType.NumberOfItems32),
                new CounterCreationData(ErrorsTransportPerSec, "The number of transport errors per second.", PerformanceCounterType.RateOfCountsPerSecond32),
            };
        }
    }
}
