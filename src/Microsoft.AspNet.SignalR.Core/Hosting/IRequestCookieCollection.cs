namespace Microsoft.AspNet.SignalR
{
    public interface IRequestCookieCollection
    {
        Cookie this[string name] { get; }
        int Count { get; }
    }
}