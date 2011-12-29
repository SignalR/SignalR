using System.Web;
using SignalR.Abstractions;

namespace SignalR.Hubs
{
    public interface IJavaScriptProxyGenerator
    {
        string GenerateProxy(string serviceUrl);
    }
}
