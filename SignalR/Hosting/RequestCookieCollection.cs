using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SignalR.Hosting
{
    public class RequestCookieCollection : IEnumerable<Cookie>
    {
        private const string ExceptionMessage_ReadOnlyCollection = "Collection is read only";
        private List<Cookie> _cookies;
        private IEnumerable<Cookie> _cookieSource;

        public RequestCookieCollection(IEnumerable<Cookie> source)
        {
            _cookieSource = source;   
        }

        public Cookie this[string name]
        {
            get
            {
                EnsureCookies();
                return _cookies.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public int Count
        {
            get
            {
                EnsureCookies();
                return _cookies.Count;
            }
        }

        public IEnumerator<Cookie> GetEnumerator()
        {
            EnsureCookies();
            return _cookies.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureCookies();
            return _cookies.GetEnumerator();
        }

        private void EnsureCookies()
        {
    		if (_cookies != null)
				return;

			_cookies = new List<Cookie>(_cookieSource ?? new Cookie[0]);
        }
    }
}
