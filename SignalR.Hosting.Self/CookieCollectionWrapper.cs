using System;
using System.Net;

namespace SignalR.Hosting.Self
{
    internal class CookieCollectionWrapper : IRequestCookieCollection, IResponseCookieCollection
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

        Cookie IRequestCookieCollection.this[string name]
        {
            get
            {
                return ToSignalRCookie(_cookies[name]);
            }
        }

        ResponseCookie IResponseCookieCollection.this[string name]
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

        void IResponseCookieCollection.Add(ResponseCookie cookie)
        {
            _cookies.Add(ToSystemNetCookie(cookie));
        }

        void IResponseCookieCollection.Clear()
        {
            if (_clearer != null)
            {
                _cookies = _clearer();
            }
        }

        private static ResponseCookie ToSignalRCookie(System.Net.Cookie source)
        {
            if (source == null)
            {
                return null;
            }

            return new ResponseCookie(
                source.Name,
                source.Value,
                source.Domain,
                source.Path,
                source.Secure,
                source.HttpOnly,
                source.Expires
            );
        }

        private static System.Net.Cookie ToSystemNetCookie(ResponseCookie source)
        {
            if (source == null)
            {
                return null;
            }

            return new System.Net.Cookie(source.Name, source.Value, source.Path, source.Domain)
                {
                    Secure = source.Secure,
                    HttpOnly = source.HttpOnly,
                    Expires = source.Expires
                };
        }
    }
}
