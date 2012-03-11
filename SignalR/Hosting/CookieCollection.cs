using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SignalR.Hosting
{
    public class CookieCollection : IEnumerable<Cookie>
    {
        private const string ExceptionMessage_ReadOnlyCollection = "Collection is read only";
        private List<Cookie> _cookies;
        private IEnumerable<Cookie> _cookieSource;
        private bool _isReadOnly = false;

        public CookieCollection()
        {
            
        }

        public CookieCollection(IEnumerable<Cookie> source)
            : this()
        {
            _cookieSource = source;   
        }

        public bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
            set
            {
                if (_isReadOnly && !value)
                {
                    // Can't change from read only to not read only
                    throw new InvalidOperationException(ExceptionMessage_ReadOnlyCollection);
                }
                _isReadOnly = value;
            }
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
            if (_cookies == null)
            {
                if (_cookieSource != null)
                {
                    _cookies = new List<Cookie>(_cookieSource);
                }
                else
                {
                    _cookieSource = new List<Cookie>();
                }
            }
        }

        private void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(ExceptionMessage_ReadOnlyCollection);
            }
        }
    }
}
