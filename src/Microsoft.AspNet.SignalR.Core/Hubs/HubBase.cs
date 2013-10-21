// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Provides methods that communicate with SignalR connections that connected to a <see cref="Hub"/>.
    /// </summary>
    public abstract class HubBase : IHub
    {
        protected HubBase()
        {
            ((IHub)this).Clients = new HubConnectionContext();
        }

        /// <summary>
        /// 
        /// </summary>
        IHubCallerConnectionContext<dynamic> IHub.Clients { get; set; }

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

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
