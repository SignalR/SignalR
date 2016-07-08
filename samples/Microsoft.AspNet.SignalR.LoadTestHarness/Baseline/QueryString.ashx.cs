// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class QueryString : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var id = context.Request.QueryString["id"];
            context.Response.ContentType = "text/plain";
            context.Response.Write("Hello World " + id);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}