using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using SignalR.Abstractions;

namespace SignalR.SelfHost
{
    public class HttpListenerRequestWrapper : IRequest
    {
        private readonly HttpListenerRequest _httpListenerRequest;
        private readonly NameValueCollection _qs;
        private NameValueCollection _form;
        private readonly NameValueCollection _headers;
        private readonly NameValueCollection _cookies;

        public HttpListenerRequestWrapper(HttpListenerRequest httpListenerRequest)
        {
            _httpListenerRequest = httpListenerRequest;
            _qs = new NameValueCollection(httpListenerRequest.QueryString);
            _headers = new NameValueCollection(httpListenerRequest.Headers);
            _cookies = new NameValueCollection();

            foreach (Cookie cookie in httpListenerRequest.Cookies)
            {
                _cookies[cookie.Name] = cookie.Value;
            }
        }

        public NameValueCollection Cookies
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

        private void EnsureForm()
        {
            if (_form == null)
            {
                _form = new NameValueCollection();

                // Do nothing if there's no body
                if (!_httpListenerRequest.HasEntityBody)
                {
                    return;
                }

                using (var sw = new StreamReader(_httpListenerRequest.InputStream))
                {
                    var body = sw.ReadToEnd();
                    string[] pairs = body.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var pair in pairs)
                    {
                        string[] entry = pair.Split('=');
                        if (entry.Length > 1)
                        {
                            _form.Add(entry[0], UrlDecode(entry[1]));
                        }
                    }
                }
            }
        }

        private static string UrlDecode(string url)
        {
            // HACK: Uri.UnescapeDataString doesn't seem to handle +
            // TODO: Copy impl from System.Web.HttpUtility.UrlDecode
            return Uri.UnescapeDataString(url).Replace("+", " ");
        }
    }
}
