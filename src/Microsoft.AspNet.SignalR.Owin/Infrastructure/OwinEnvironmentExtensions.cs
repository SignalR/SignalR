using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Owin
{
    internal static class OwinEnvironmentExtensions
    {
        internal static CancellationToken GetShutdownToken(this IDictionary<string, object> env)
        {
            object value;
            return env.TryGetValue(OwinConstants.HostOnAppDisposing, out value)
                && value is CancellationToken
                ? (CancellationToken)value
                : default(CancellationToken);
        }

        internal static string GetAppInstanceName(this IDictionary<string, object> env)
        {
            object value;
            if (env.TryGetValue(OwinConstants.HostAppNameKey, out value))
            {
                var stringVal = value as string;

                if (!String.IsNullOrEmpty(stringVal))
                {
                    return stringVal;
                }
            }

            return null;
        }

        internal static bool SupportsWebSockets(this IDictionary<string, object> env)
        {
            object value;
            if (env.TryGetValue(OwinConstants.ServerCapabilities, out value))
            {
                var capabilities = value as IDictionary<string, object>;
                if (capabilities != null)
                {
                    return capabilities.ContainsKey(OwinConstants.WebSocketVersion);
                }
            }
            return false;
        }

        internal static bool GetIsDebugEnabled(this IDictionary<string, object> env)
        {
            object value;
            if (env.TryGetValue(OwinConstants.HostAppModeKey, out value))
            {
                var stringVal = value as string;
                return !String.IsNullOrWhiteSpace(stringVal) &&
                       OwinConstants.AppModeDevelopment.Equals(stringVal, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
