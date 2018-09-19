// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class MyItemsHub : Hub
    {
        public Task GetItems()
        {
            return PrintEnvironment("GetItems", Context.Request);
        }

        public override Task OnConnected()
        {
            return PrintEnvironment("OnConnected", Context.Request);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            return PrintEnvironment("OnDisconnected", Context.Request);
        }

        private Task PrintEnvironment(string method, IRequest request)
        {
            var responseHeaders = (IDictionary<string, string[]>)request.Environment["owin.ResponseHeaders"];
            return Clients.All.update(new
            {
                method = method,
                count = request.Environment.Count,
                owinKeys = request.Environment.Keys,
                headers = request.Headers,
                query = request.QueryString,
                xContentTypeOptions = responseHeaders["X-Content-Type-Options"][0]
            });
        }
    }
}
