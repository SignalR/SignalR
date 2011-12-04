using System.Web;

namespace SignalR
{
    public interface IConnectionIdFactory
    {
        string CreateConnectionId(HttpContextBase context);
    }
}
