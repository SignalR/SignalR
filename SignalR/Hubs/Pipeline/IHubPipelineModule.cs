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
        Func<IHub, IEnumerable<string>, Task> BuildReconnect(Func<IHub, IEnumerable<string>, Task> reconnect);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disconnect"></param>
        /// <returns></returns>
        Func<IHub, Task> BuildDisconnect(Func<IHub, Task> disconnect);
    }
}
