using System.Web;
using SignalR.Hosting;

namespace SignalR.Hubs
{
    public interface IJavaScriptProxyGenerator
    {
        string GenerateProxy(string serviceUrl);
    }
}
