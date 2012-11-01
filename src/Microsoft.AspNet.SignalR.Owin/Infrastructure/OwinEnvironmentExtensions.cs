using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Owin
{
    internal static class OwinEnvironmentExtensions
    {
        public static CancellationToken GetShutdownToken(this IDictionary<string, object> env)
        {
            object value;
            return env.TryGetValue(OwinConstants.HostOnAppDisposing, out value)
                && value is CancellationToken
                ? (CancellationToken)value
                : default(CancellationToken);
        }

        public static string GetAppInstanceName(this IDictionary<string, object> env)
        {
            object value;
            return env.TryGetValue(OwinConstants.HostAppNameKey, out value)
                && value is string
                ? (string)value
                : default(string);
        }
    }
}
