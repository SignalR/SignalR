using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class ExamineHeadersConnection : PersistentConnection
    {
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            string refererHeader = request.Headers[System.Net.HttpRequestHeader.Referer.ToString()];
            string testHeader = request.Headers["test-header"];

            return Connection.Send(connectionId, new
            {
                refererHeader = refererHeader,
                testHeader = testHeader
            });
        }
    }
}
