using System;

namespace Microsoft.AspNet.SignalR.Configuration
{
    internal static class ConfigurationExtensions
    {
        internal const int MissedTimeoutsBeforeClientReconnect = 2;
        internal const int HeartBeatsPerKeepAlive = 2;

        /// <summary>
        /// The amount of time the client should wait without seeing a keep alive before trying to reconnect.
        /// </summary>
        public static TimeSpan KeepAliveTimeout(this IConfigurationManager config)
        {
            return TimeSpan.FromTicks(config.KeepAlive.Ticks * MissedTimeoutsBeforeClientReconnect);
        }

        /// <summary>
        /// The interval between successively checking connection states.
        /// </summary>
        public static TimeSpan HeartbeatInterval(this IConfigurationManager config)
        {
            var keepAliveTicks = config.KeepAlive.Ticks;
            if (keepAliveTicks != 0)
            {
                return TimeSpan.FromTicks(config.KeepAlive.Ticks / HeartBeatsPerKeepAlive);
            }
            else
            {
                // If KeepAlives are disabled, have the heartbeat run every ten seconds. 
                return TimeSpan.FromSeconds(10);
            }
        }
    }
}
