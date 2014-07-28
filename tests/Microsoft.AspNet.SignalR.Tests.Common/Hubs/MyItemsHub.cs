using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

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
