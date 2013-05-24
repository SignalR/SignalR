using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class ParamHub : Hub
    {
        public void getRequest(params string[] args)
        {
            Clients.All.display("From getRequest(params string[] args)");
        }

        public void getRequest(string test = "111")
        {
            Clients.All.display("From getRequest(string test = " + test + ")");
        }

        public void getRequest(string test, params string[] args)
        {
            Clients.All.display("From getRequest(string test, params string[] args)");
        }

        public void getRequest(string test, string test1 = "", params string[] args)
        {
            Clients.All.display(@"From getRequest(string test, string test1 = , params string[] args)");
        }

        public void getRequest(string test, string test1, bool test2 = true, bool test3 = false)
        {
            Clients.All.display("From getRequest(string test, string test1, bool test2 = true, bool test3 = false)");
        }

    }
}
