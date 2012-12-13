using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class MyBadConnection : PersistentConnection
    {
        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            // Should throw 404
            using (HttpWebRequest.Create("http://www.microsoft.com/mairyhadalittlelambbut_shelikedhertwinkling_littlestar_better").GetResponse()) { }

            return base.OnConnectedAsync(request, connectionId);
        }
    }
}
