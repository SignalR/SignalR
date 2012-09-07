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
                if (OnBeforeIncoming(context))
                {
                    return invoke(context).Then(result => OnAfterIncoming(result, context))
                                          .Catch(ex => OnIncomingError(ex));
                }

                return TaskAsyncHelper.FromResult<object>(null);
            };
        }

        public virtual Func<IHub, Task> BuildConnect(Func<IHub, Task> connect)
        {
            return hub =>
            {
                if (OnBeforeConnect(hub))
                {
                    return connect(hub).Then(h => OnAfterConnect(h), hub);
                }

                return TaskAsyncHelper.Empty;
            };
        }

        public virtual Func<IHub, IEnumerable<string>, Task> BuildReconnect(Func<IHub, IEnumerable<string>, Task> reconnect)
        {
            return (hub, groups) =>
            {
                if (OnBeforeReconnect(hub, groups))
                {
                    return reconnect(hub, groups).Then((h, g) => OnAfterReconnect(h, g), hub, groups);
                }
                return TaskAsyncHelper.Empty;
            };
        }

        public virtual Func<IHub, Task> BuildDisconnect(Func<IHub, Task> disconnect)
        {
            return hub =>
            {
                if (OnBeforeDisconnect(hub))
                {
                    return disconnect(hub).Then(h => OnAfterDisconnect(h), hub);
                }

                return TaskAsyncHelper.Empty;
            };
        }

        public Func<IHubOutgoingInvokerContext, Task> BuildOutgoing(Func<IHubOutgoingInvokerContext, Task> send)
        {
            return context =>
            {
                if (OnBeforeOutgoing(context))
                {
                    return send(context).Then(ctx => OnAfterOutgoing(ctx), context);
                }

                return TaskAsyncHelper.Empty;
            };
        }

        protected virtual bool OnBeforeConnect(IHub hub)
        {
            return true;
        }

        protected virtual void OnAfterConnect(IHub hub)
        {

        }

        protected virtual bool OnBeforeReconnect(IHub hub, IEnumerable<string> groups)
        {
            return true;
        }

        protected virtual void OnAfterReconnect(IHub hub, IEnumerable<string> groups)
        {

        }

        protected virtual bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            return true;
        }

        protected virtual void OnAfterOutgoing(IHubOutgoingInvokerContext context)
        {

        }

        protected virtual bool OnBeforeDisconnect(IHub hub)
        {
            return true;
        }

        protected virtual void OnAfterDisconnect(IHub hub)
        {

        }

        protected virtual bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            return true;
        }

        protected virtual object OnAfterIncoming(object result, IHubIncomingInvokerContext context)
        {
            return result;
        }

        protected virtual void OnIncomingError(Exception ex)
        {

        }
    }
}
