using System;

namespace SignalR
{
    public class DefaultConfigurationManager : IConfigurationManager
    {
        public DefaultConfigurationManager()
        {
            ReconnectionTimeout = TimeSpan.FromSeconds(110);
            DisconnectTimeout = TimeSpan.FromSeconds(20);
            HeartBeatInterval = TimeSpan.FromSeconds(10);
        }

        public TimeSpan ReconnectionTimeout
        {
            get;
            set;
        }

        public TimeSpan DisconnectTimeout
        {
            get;
            set;
        }

        public TimeSpan HeartBeatInterval
        {
            get;
            set;
        }
    }
}
