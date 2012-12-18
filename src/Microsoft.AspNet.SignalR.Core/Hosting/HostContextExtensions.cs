﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNet.SignalR
{
    public static class HostContextExtensions
    {
        public static T GetValue<T>(this HostContext context, string key)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            object value;
            if (context.Items.TryGetValue(key, out value))
            {
                return (T)value;
            }
            return default(T);
        }

        public static bool IsDebuggingEnabled(this HostContext context)
        {
            return context.GetValue<bool>(HostConstants.DebugMode);
        }

        public static bool SupportsWebSockets(this HostContext context)
        {
            return context.GetValue<bool>(HostConstants.SupportsWebSockets);
        }

        public static string WebSocketServerUrl(this HostContext context)
        {
            return context.GetValue<string>(HostConstants.WebSocketServerUrl);
        }

        public static CancellationToken HostShutdownToken(this HostContext context)
        {
            return context.GetValue<CancellationToken>(HostConstants.ShutdownToken);
        }

        public static string InstanceName(this HostContext context)
        {
            return context.GetValue<string>(HostConstants.InstanceName);
        }
    }
}
