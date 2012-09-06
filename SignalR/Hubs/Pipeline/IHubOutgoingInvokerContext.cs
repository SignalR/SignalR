namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubOutgoingInvokerContext
    {
        /// <summary>
        /// 
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// 
        /// </summary>
        ClientHubInvocation Invocation { get; }

        /// <summary>
        /// 
        /// </summary>
        string Signal { get; }
    }
}
