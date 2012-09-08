using System.Collections.Generic;

namespace SignalR.Server
{
    class ServerRequestCookieCollection : IRequestCookieCollection
    {
        private readonly IDictionary<string, Cookie> _cookies;

        public ServerRequestCookieCollection(IDictionary<string, Cookie> cookies)
        {
            _cookies = cookies;
        }

        public Cookie this[string name]
        {
            get
            {
                Cookie value;
                return _cookies.TryGetValue(name, out value) ? value : null;
            }
        }

        public int Count
        {
            get { return _cookies.Count; }
        }
    }
}