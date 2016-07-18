// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    [HubName("ExamineHeadersHub")]
    public class ExamineHeadersHub : Hub
    {
        public Task Send()
        {
            string testHeader = Context.Headers.Get("test-header");
            string refererHeader = Context.Headers.Get(HttpRequestHeader.Referer.ToString());

            return Clients.Caller.sendHeader(new
            {
                refererHeader = refererHeader,
                testHeader = testHeader
            });
        }
    }
}
