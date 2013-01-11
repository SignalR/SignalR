using System;

namespace Microsoft.AspNet.SignalR.Configuration
{
    internal static class ConfigurationExtensions
    {
        internal const int MissedTimeoutsBeforeClientReconnect = 2;
        internal const int HeartBeatsPerKeepAlive = 2;
        internal const int HeartBeatsPerDisconnectTimeout = 6;

        /// <summary>
        /// The amount of time the client should wait without seeing a keep alive before trying to reconnect.
        /// </summary>
        public static TimeSpan? KeepAliveTimeout(this IConfigurationManager config)
        {
            if (config.KeepAlive != null)
            {
                return TimeSpan.FromTicks(config.KeepAlive.Value.Ticks * MissedTimeoutsBeforeClientReconnect);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// The interval between successively checking connection states.
        /// </summary>
        public static TimeSpan HeartbeatInterval(this IConfigurationManager config)
        {
            if (config.KeepAlive != null)
            {
                return TimeSpan.FromTicks(config.KeepAlive.Value.Ticks / HeartBeatsPerKeepAlive);
            }
            else
            {
                // If KeepAlives are disabled, have the heartbeat run at the same rate it would if the KeepAlive was
                // kept at the default value.
                return TimeSpan.FromTicks(config.DisconnectTimeout.Ticks / HeartBeatsPerDisconnectTimeout);
            }
        }
    }
}
