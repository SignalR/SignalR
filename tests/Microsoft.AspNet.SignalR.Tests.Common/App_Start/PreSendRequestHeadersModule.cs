// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Web;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class PreSendRequestHeadersModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PreSendRequestHeaders += (s, e) => { };
        }

        public void Dispose()
        {
        }
    }
}
