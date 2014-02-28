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
