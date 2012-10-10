using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubPipelineInvoker
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<object> Invoke(IHubIncomingInvokerContext context);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task Send(IHubOutgoingInvokerContext context);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hub"></param>
        /// <returns></returns>
        Task Connect(IHub hub);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hub"></param>
        /// <returns></returns>
        Task Reconnect(IHub hub);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hub"></param>
        /// <returns></returns>
        Task Disconnect(IHub hub);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hubDescriptor"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        bool AuthorizeConnect(HubDescriptor hubDescriptor, IRequest request);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="groups"></param>
        /// <returns></returns>
        IEnumerable<string> RejoiningGroups(IHub hub, IEnumerable<string> groups);
    }
}
