using System.Web;

namespace SignalR
{
    public interface IClientIdFactory
    {
        string CreateClientId(HttpContextBase context);
    }
}
