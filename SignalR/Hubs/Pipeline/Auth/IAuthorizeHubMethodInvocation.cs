namespace SignalR.Hubs
{
    /// <summary>
    /// Interface to be implemented by <see cref="System.Attribute"/>s that can authorize the invocation of <see cref="IHub"/> methods.
    /// </summary>
    public interface IAuthorizeHubMethodInvocation
    {
        /// <summary>
        /// Given a <see cref="IHubIncomingInvokerContext"/>, determine whether client is authorized to invoke the <see cref="IHub"/> method.
        /// </summary>
        /// <param name="hubIncomingInvokerContext">An <see cref="IHubIncomingInvokerContext"/> providing details regarding the <see cref="IHub"/> method invocation.</param>
        /// <returns>true if the caller is authorized to invoke the <see cref="IHub"/> method; otherwise, false.</returns>
        bool AuthorizeHubMethodInvocation(IHubIncomingInvokerContext hubIncomingInvokerContext);
    }
}
