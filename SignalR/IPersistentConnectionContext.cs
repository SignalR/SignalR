using System;

namespace SignalR
{
    /// <summary>
    /// Provides access to information about a persistent connection
    /// </summary>
    public interface IPersistentConnectionContext
    {
        /// <summary>
        /// Gets the <see cref="IConnection" /> for the <see cref="PersistentConnection" />
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Gets the <see cref="IGroupManager" /> for the <see cref="PersistentConnection" />
        /// </summary>
        IGroupManager Groups { get; }
    }
}
