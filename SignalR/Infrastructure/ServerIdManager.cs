using System;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Default <see cref="IServerIdManager"/> implementation.
    /// </summary>
    public class ServerIdManager : IServerIdManager
    {
        public ServerIdManager()
        {
            ServerId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// The id of the server.
        /// </summary>
        public string ServerId
        {
            get;
            private set;
        }
    }
}
