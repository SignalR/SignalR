using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal static class HubOutgoingInvokerContextExtensions
    {
        public static ConnectionMessage GetConnectionMessage(this IHubOutgoingInvokerContext context)
        {
            if (String.IsNullOrEmpty(context.Signal))
            {
                return new ConnectionMessage(context.Signals, context.Invocation, context.ExcludedSignals);
            }
            else
            {
                return new ConnectionMessage(context.Signal, context.Invocation, context.ExcludedSignals);
            }
        }
    }
}
