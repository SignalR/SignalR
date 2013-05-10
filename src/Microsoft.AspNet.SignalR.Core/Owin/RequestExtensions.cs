// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Owin;

namespace Microsoft.AspNet.SignalR
{
    internal static class RequestExtensions
    {
        internal static T Get<T>(this IDictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) ? (T)value : default(T);
        }
    }
}
