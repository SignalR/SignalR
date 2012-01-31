using System;

namespace SignalR.Configuration
{
    public class ConfigurationManager : IConfigurationManager
    {
        public ConfigurationManager()
        {
            ReconnectionTimeout = TimeSpan.FromSeconds(110);
            DisconnectTimeout = TimeSpan.FromSeconds(20);
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
    }
}
