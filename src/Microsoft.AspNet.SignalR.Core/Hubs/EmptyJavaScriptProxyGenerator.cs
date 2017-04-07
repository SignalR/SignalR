// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class EmptyJavaScriptProxyGenerator : IJavaScriptProxyGenerator
    {
        public string GenerateProxy(string serviceUrl)
        {
            return String.Format(CultureInfo.InvariantCulture, "throw new Error('{0}');", Resources.Error_JavaScriptProxyDisabled);
        }
    }
}
