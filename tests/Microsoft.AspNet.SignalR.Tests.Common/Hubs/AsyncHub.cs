using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class AsyncHub : Hub
    {
        public override async Task OnConnected()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        public async Task<string> Echo(string message)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return message;
        }
    }
}
