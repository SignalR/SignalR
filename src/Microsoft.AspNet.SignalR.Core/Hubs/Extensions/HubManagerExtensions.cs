// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public static class HubManagerExtensions
    {
        public static HubDescriptor EnsureHub(this IHubManager hubManager, string hubName, params IPerformanceCounter[] counters)
        {
            var descriptor = hubManager.GetHub(hubName);

            if (descriptor == null)
            {
                for (var i = 0; i < counters.Length; i++)
                {
                    counters[i].Increment();
                }
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Errror_HubCouldNotBeResolved, hubName));
            }

            return descriptor;
        }
    }
}
