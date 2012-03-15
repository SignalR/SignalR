using System.Web;

namespace SignalR.Hosting.AspNet
{
    internal class HttpCookieCollectionWrapper : IRequestCookieCollection, IResponseCookieCollection
    {
        private readonly HttpCookieCollection _cookies;

        public HttpCookieCollectionWrapper(HttpCookieCollection cookies)
        {
            _cookies = cookies;
        }

        Cookie IRequestCookieCollection.this[string name]
        {
            get { return ToSignalRCookie(_cookies[name]); }
        }

        ResponseCookie IResponseCookieCollection.this[string name]
        {
            get { return ToSignalRCookie(_cookies[name]); }
        }

        public int Count
        {
            get { return _cookies.Count; }
        }

        void IResponseCookieCollection.Add(ResponseCookie cookie)
        {
            _cookies.Add(ToSystemWebCookie(cookie));
        }

        void IResponseCookieCollection.Clear()
        {
            _cookies.Clear();
        }

        private static ResponseCookie ToSignalRCookie(HttpCookie source)
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

        private static HttpCookie ToSystemWebCookie(ResponseCookie source)
        {
            if (source == null)
            {
                return null;
            }

            return new HttpCookie(source.Name, source.Value)
                {
                    Domain = source.Domain,
                    Path = source.Path,
                    Secure = source.Secure,
                    HttpOnly = source.HttpOnly,
                    Expires = source.Expires
                };
        }
    }
}
