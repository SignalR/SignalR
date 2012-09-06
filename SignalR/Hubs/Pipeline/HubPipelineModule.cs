using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class HubPipelineModule : IHubPipelineModule
    {
        public virtual Func<IHubIncomingInvokerContext, Task<object>> BuildIncoming(Func<IHubIncomingInvokerContext, Task<object>> invoke)
        {
            return context =>
            {
                OnBeforeInvoke(context);
                return invoke(context).Then(result =>
                {
                    OnAfterInvoke(result, context);
                    return result;
                })
                .Catch(ex => OnInvokeError(ex));
            };
        }

        public virtual Func<IHub, Task> BuildConnect(Func<IHub, Task> connect)
        {
            return connect;
        }

        public virtual Func<IHub, IEnumerable<string>, Task> BuildReconnect(Func<IHub, IEnumerable<string>, Task> reconnect)
        {
            return reconnect;
        }

        public virtual Func<IHub, Task> BuildDisconnect(Func<IHub, Task> disconnect)
        {
            return disconnect;
        }

        public Func<IHubOutgoingInvokerContext, Task> BuildOutgoing(Func<IHubOutgoingInvokerContext, Task> send)
        {
            return context =>
            {
                OnBeforeOutgoing(context);
                return send(context);
            };
        }

        protected virtual void OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            
        }

        protected virtual void OnBeforeInvoke(IHubIncomingInvokerContext context)
        {

        }

        protected virtual void OnAfterInvoke(object result, IHubIncomingInvokerContext context)
        {

        }

        protected virtual void OnInvokeError(Exception ex)
        {

        }
    }
}
