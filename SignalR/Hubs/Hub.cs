using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    /// <summary>
    /// Provides methods that communicate with SignalR connections that connected to a <see cref="Hub"/>.
    /// </summary>
    public abstract class Hub : IHub
    {
        protected Hub()
        {
            Clients = new HubConnectionContext();
            Clients.All = new NullClientProxy();
            Clients.Others = new NullClientProxy();
            Clients.Caller = new NullClientProxy();
        }

        /// <summary>
        /// 
        /// </summary>
        public HubConnectionContext Clients { get; set; }

        /// <summary>
        /// Provides information about the calling client.
        /// </summary>
        public HubCallerContext Context { get; set; }

        /// <summary>
        /// The group manager for this hub instance.
        /// </summary>
        public IGroupManager Groups { get; set; }

        /// <summary>
        /// Called when a connection disconnects from this hub instance.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public virtual Task OnDisconnected()
        {
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when the connection connects to this hub instance.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public virtual Task OnConnected()
        {
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when the connection reconnects to this hub instance.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public virtual Task OnReconnected()
        {
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called before a connection completes reconnecting to this <see cref="IHub"/> instance.
        /// </summary>
        /// <param name="groups">The groups the reconnecting client claims to be a member of.</param>
        /// <returns>The groups the client will actually join.</returns>
        public virtual IEnumerable<string> RejoiningGroups(IEnumerable<string> groups)
        {
            return Enumerable.Empty<string>();
        }

        public virtual void Dispose()
        {
        }
    }
}