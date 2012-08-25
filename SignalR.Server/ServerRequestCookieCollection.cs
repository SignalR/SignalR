
namespace SignalR.Server
{
    class ServerRequestCookieCollection : IRequestCookieCollection
    {
        readonly Gate.Request _req;

        public ServerRequestCookieCollection(Gate.Request req)
        {
            _req = req;
        }

        public Cookie this[string name]
        {
            get
            {
                string value;
                return _req.Cookies.TryGetValue(name, out value) ? new Cookie(name, value) : null;
            }
        }

        public int Count
        {
            get { return _req.Cookies.Count; }
        }
    }
}