
namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubIncomingInvokerContext
    {
        /// <summary>
        /// 
        /// </summary>
        IHub Hub { get; }

        /// <summary>
        /// 
        /// </summary>
        MethodDescriptor MethodDescriptor { get; }

        /// <summary>
        /// 
        /// </summary>
        object[] Args { get; }

        /// <summary>
        /// 
        /// </summary>
        TrackingDictionary State { get; }
    }
}
