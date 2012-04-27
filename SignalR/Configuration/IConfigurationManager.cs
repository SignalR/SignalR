using System;

namespace SignalR
{
    /// <summary>
    /// Provides access to server configuration.
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Gets of sets a <see cref="TimeSpan"/> representing the amount of time to leave a connection open before timing out.
        /// </summary>
        TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets of sets a <see cref="TimeSpan"/> representing the amount of time to wait after a connection goes away before raising the disconnect event.
        /// </summary>
        TimeSpan DisconnectTimeout { get; set; }

        /// <summary>
        /// Gets of sets a <see cref="TimeSpan"/> representing the interval for checking the state of a connection. 
        /// </summary>
        TimeSpan HeartBeatInterval { get; set; }

        /// <summary>
        /// Gets of sets a <see cref="TimeSpan"/> representing the amount of time to wait before sending a keep alive packet over an idle connection. Set to null to disable keep alive.
        /// </summary>
        TimeSpan? KeepAlive { get; set; }
    }
}
