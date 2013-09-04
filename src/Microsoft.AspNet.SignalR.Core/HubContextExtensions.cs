using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR
{
    public static class HubContextExtensions
    {
        /// <summary>
        /// Subscribe to client invocations on the <see cref="IHubContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="IHubContext"/>.</param>
        /// <param name="callback">The callback that will be called when an invocation is received.</param>
        /// <returns>A <see cref="IDisposable"/> that when called terminates the subscription.</returns>
        public static IDisposable Subscribe(this IHubContext context, Func<ClientHubInvocation, Task> callback)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            return context.Connection.Receive(async message =>
            {
                var invocation = message.ToObject<ClientHubInvocation>(context.Serializer);

                await callback(invocation);
            });
        }
    }
}
