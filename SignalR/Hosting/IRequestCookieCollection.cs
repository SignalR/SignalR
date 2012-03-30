
namespace SignalR.Hosting
{
    public interface IRequestCookieCollection
    {
        Cookie this[string name] { get; }
        int Count { get; }
    }
}