using Gate;

namespace SignalR.Server
{
    class ServerRequestCookieCollection : IRequestCookieCollection
    {
        readonly Request _req;

        public ServerRequestCookieCollection(Request req)
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