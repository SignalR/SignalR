// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Web;
using System.Web.Routing;

namespace Microsoft.AspNet.SignalR.Tests.Common.Handlers
{
    public class GCHandler : IRouteHandler, IHttpHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return this;
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
