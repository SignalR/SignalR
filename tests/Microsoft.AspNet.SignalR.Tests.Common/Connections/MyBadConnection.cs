using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class MyBadConnection : PersistentConnection
    {
        public override Task ProcessRequest(Hosting.HostContext context)
        {

            return base.ProcessRequest(context);
        }
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            // Should throw 404
            using (HttpWebRequest.Create("http://www.microsoft.com/mairyhadalittlelambbut_shelikedhertwinkling_littlestar_better").GetResponse()) { }

            return base.OnConnected(request, connectionId);
        }
    }
}
