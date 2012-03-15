
namespace SignalR.Hosting
{
    public interface IResponseCookieCollection
    {
        ResponseCookie this[string name] { get; }
        int Count { get; }
        void Add(ResponseCookie cookie);
        void Clear();
    }
}
