using System;

namespace SignalR.Configuration
{
    public interface IConfigurationManager
    {
        TimeSpan ReconnectionTimeout { get; set; }
        TimeSpan DisconnectTimeout { get; set; }
    }
}
