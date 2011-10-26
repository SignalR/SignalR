using System.Web;

namespace SignalR.Hubs
{
    public interface IJavaScriptProxyGenerator
    {
        string GenerateProxy(HttpContextBase context, string serviceUrl);
    }
}
