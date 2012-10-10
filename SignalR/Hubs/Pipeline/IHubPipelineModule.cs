using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubPipelineModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="invoke"></param>
        /// <returns></returns>
        Func<IHubIncomingInvokerContext, Task<object>> BuildIncoming(Func<IHubIncomingInvokerContext, Task<object>> invoke);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="send"></param>
        /// <returns></returns>
        Func<IHubOutgoingInvokerContext, Task> BuildOutgoing(Func<IHubOutgoingInvokerContext, Task> send);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connect"></param>
        /// <returns></returns>
        Func<IHub, Task> BuildConnect(Func<IHub, Task> connect);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reconnect"></param>
        /// <returns></returns>
        Func<IHub, Task> BuildReconnect(Func<IHub, Task> reconnect);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disconnect"></param>
        /// <returns></returns>
        Func<IHub, Task> BuildDisconnect(Func<IHub, Task> disconnect);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorizeConnect"></param>
        /// <returns></returns>
        Func<HubDescriptor, IRequest, bool> BuildAuthorizeConnect(Func<HubDescriptor, IRequest, bool> authorizeConnect);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rejoiningGroups"></param>
        /// <returns></returns>
        Func<IHub, IEnumerable<string>, IEnumerable<string>> BuildRejoiningGroups(Func<IHub, IEnumerable<string>, IEnumerable<string>> rejoiningGroups);
    }
}
