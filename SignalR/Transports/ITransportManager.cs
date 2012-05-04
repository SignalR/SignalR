using SignalR.Hosting;

namespace SignalR.Transports
{
    /// <summary>
    /// Manages the transports for connections.
    /// </summary>
    public interface ITransportManager
    {
        /// <summary>
        /// Gets the specified transport for the specified <see cref="HostContext"/>.
        /// </summary>
        /// <param name="hostContext">The <see cref="HostContext"/> for the current request.</param>
        /// <returns>The <see cref="ITransport"/> for the specified <see cref="HostContext"/>.</returns>
        ITransport GetTransport(HostContext hostContext);
        
        /// <summary>
        /// Determines whether the specified transport is supported.
        /// </summary>
        /// <param name="transportName">The name of the transport to test.</param>
        /// <returns>True if the transport is supported, otherwise False.</returns>
        bool SupportsTransport(string transportName);
    }
}
