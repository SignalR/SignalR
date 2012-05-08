using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using SignalR.Hosting.Common;
using System.Security.Principal;

namespace SignalR.Hosting.Self
{
    public class HttpListenerRequestWrapper : IRequest
    {
        private readonly HttpListenerRequest _httpListenerRequest;
        private readonly NameValueCollection _qs;
        private NameValueCollection _form;
        private readonly NameValueCollection _headers;
        private readonly CookieCollectionWrapper _cookies;

        public HttpListenerRequestWrapper(HttpListenerRequest httpListenerRequest, IPrincipal user)
        {
            _httpListenerRequest = httpListenerRequest;
            _qs = new NameValueCollection(httpListenerRequest.QueryString);
            _headers = new NameValueCollection(httpListenerRequest.Headers);
            _cookies = new CookieCollectionWrapper(_httpListenerRequest.Cookies);
            User = user;
        }

        public IRequestCookieCollection Cookies
        {
            get
            {
                return _cookies;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                EnsureForm();
                return _form;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return _headers;
            }
        }

        public Uri Url
        {
            get
            {
                return _httpListenerRequest.Url;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                return _qs;
            }
        }

        public IPrincipal User
        {
            get;
            private set;
        }

        private void EnsureForm()
        {
            if (_form == null)
            {
                // Do nothing if there's no body
                if (!_httpListenerRequest.HasEntityBody)
                {
                    _form = new NameValueCollection();
                    return;
                }

                using (var sw = new StreamReader(_httpListenerRequest.InputStream))
                {
                    var body = sw.ReadToEnd();
                    _form = HttpUtility.ParseDelimited(body);
                }
            }
        }
    }
}
