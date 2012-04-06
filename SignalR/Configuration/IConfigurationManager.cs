using System;

namespace SignalR
{
    public interface IConfigurationManager
    {
        TimeSpan ReconnectionTimeout { get; set; }
        TimeSpan DisconnectTimeout { get; set; }
        TimeSpan HeartBeatInterval { get; set; }
        TimeSpan? KeepAlive { get; set; }
    }
}
