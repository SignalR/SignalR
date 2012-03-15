using System;
using System.Net;

namespace SignalR.Hosting.Self
{
    internal class CookieCollectionWrapper : IRequestCookieCollection
    {
        private CookieCollection _cookies;
        private readonly Func<CookieCollection> _clearer;

        public CookieCollectionWrapper(CookieCollection cookies)
            : this (cookies, null)
        {

        }

        public CookieCollectionWrapper(CookieCollection cookies, Func<CookieCollection> clearer)
        {
            _cookies = cookies;
            _clearer = clearer;
        }

        public Cookie this[string name]
        {
            get
            {
                return ToSignalRCookie(_cookies[name]);
            }
        }

        public int Count
        {
            get { return _cookies.Count; }
        }

        private static Cookie ToSignalRCookie(System.Net.Cookie source)
        {
            if (source == null)
            {
                return null;
            }

            return new Cookie(
                source.Name,
                source.Value,
                source.Domain,
                source.Path
            );
        }
    }
}
