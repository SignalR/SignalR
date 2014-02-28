// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Microsoft.AspNet.SignalR
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

        internal static string GetAppInstanceName(this IDictionary<string, object> environment)
        {
            object value;
            if (environment.TryGetValue(OwinConstants.HostAppNameKey, out value))
            {
                var stringVal = value as string;

                if (!String.IsNullOrEmpty(stringVal))
                {
                    return stringVal;
                }
            }

            return null;
        }

        internal static TextWriter GetTraceOutput(this IDictionary<string, object> environment)
        {
            object value;
            if (environment.TryGetValue(OwinConstants.HostTraceOutputKey, out value))
            {
                return value as TextWriter;
            }

            return null;
        }

        internal static bool SupportsWebSockets(this IDictionary<string, object> environment)
        {
            object value;
            if (environment.TryGetValue(OwinConstants.ServerCapabilities, out value))
            {
                var capabilities = value as IDictionary<string, object>;
                if (capabilities != null)
                {
                    return capabilities.ContainsKey(OwinConstants.WebSocketVersion);
                }
            }
            return false;
        }

        internal static bool IsDebugEnabled(this IDictionary<string, object> environment)
        {
            object value;
            if (environment.TryGetValue(OwinConstants.HostAppModeKey, out value))
            {
                var stringVal = value as string;
                return !String.IsNullOrWhiteSpace(stringVal) &&
                       OwinConstants.AppModeDevelopment.Equals(stringVal, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        internal static IEnumerable<Assembly> GetReferenceAssemblies(this IDictionary<string, object> environment)
        {
            object assembliesValue;
            if (environment.TryGetValue(OwinConstants.HostReferencedAssembliesKey, out assembliesValue))
            {
                return (IEnumerable<Assembly>)assembliesValue;
            }

            return null;
        }

        internal static void DisableResponseBuffering(this IDictionary<string, object> environment)
        {
            var action = environment.Get<Action>(OwinConstants.DisableResponseBuffering);

            if (action != null)
            {
                action();
            }
        }

        internal static void DisableRequestCompression(this IDictionary<string, object> environment)
        {
            var action = environment.Get<Action>(OwinConstants.DisableRequestCompression);

            if (action != null)
            {
                action();
            }
        }
    }
}
