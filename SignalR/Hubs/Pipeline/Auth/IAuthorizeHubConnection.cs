namespace SignalR.Hubs
{
    /// <summary>
    /// Interface to be implemented by <see cref="System.Attribute"/>s that can authorize client to connect to a <see cref="IHub"/>.
    /// </summary>
    public interface IAuthorizeHubConnection
    {
        /// <summary>
        /// Given a <see cref="HubCallerContext"/>, determine whether client is authorized to connect to <see cref="IHub"/>.
        /// </summary>
        /// <param name="hub">The <see cref="IHub"/> the client is attempting to connect to. Request information can be found in <see cref="IHub.Context"/>.</param>
        /// <returns>true if the caller is authorized to connect to the <see cref="IHub"/>; otherwise, false.</returns>
        bool AuthorizeHubConnection(IHub hub);
    }
}
