namespace SignalR.Hubs
{
    public interface IJavaScriptProxyGenerator
    {
        string GenerateProxy(string serviceUrl, bool includeDocComments = false);
    }
}
