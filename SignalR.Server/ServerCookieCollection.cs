using System;
using Gate;

namespace SignalR.Server
{
    class ServerCookieCollection : IRequestCookieCollection
    {
        readonly Request _req;

        public ServerCookieCollection(Request req)
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