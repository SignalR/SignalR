using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Hubs
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

        public override Task OnDisconnected()
        {
            return PrintEnvironment("OnDisconnected", Context.Request);
        }

        private Task PrintEnvironment(string method, IRequest request)
        {
            object owinEnv;
            if (request.Items.TryGetValue("owin.environment", out owinEnv))
            {
                var env = (IDictionary<string, object>)owinEnv;
                var responseHeaders = (Dictionary<string, string[]>)env["owin.ResponseHeaders"];
                return Clients.All.update(new
                {
                    method = method,
                    count = env.Count,
                    owinKeys = env.Keys,
                    keys = request.Items.Keys,
                    xContentTypeOptions = responseHeaders["X-Content-Type-Options"][0]
                });
            }

            return Clients.All.update(new
            {
                method = method,
                count = 0,
                keys = new string[0],
                owinKeys = new string[0],
                xContentTypeOptions = ""
            });
        }
    }
}
