using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using SignalR.Client.Infrastructure;

namespace SignalR.Hosting.Memory
{
    public class Request : IHttpRequest, IRequest
    {
        private readonly CancellationTokenSource _clientTokenSource;

        public Request(Uri uri, CancellationTokenSource clientTokenSource, Dictionary<string, string> postData)
        {
            Url = uri;
            _clientTokenSource = clientTokenSource;
            Form = new NameValueCollection();
            Headers = new NameValueCollection();
            QueryString = ParseDelimited(Url.Query.TrimStart('?'));

            if (postData != null)
            {
                foreach (var pair in postData)
                {
                    Form[pair.Key] = pair.Value;
                }
            }
        }

        public string UserAgent
        {
            get
            {
                return Headers["User-Agent"];
            }
            set
            {
                Headers["UserAgent"] = value;
            }
        }

        public ICredentials Credentials { get; set; }

        public CookieContainer CookieContainer { get; set; }

        public string Accept
        {
            get
            {
                return Headers["Accept"];
            }
            set
            {
                Headers["Accept"] = value;
            }
        }

        public void Abort()
        {
            _clientTokenSource.Cancel();
        }

        public Uri Url
        {
            get;
            private set;
        }

        public NameValueCollection QueryString
        {
            get;
            private set;
        }

        public NameValueCollection Headers
        {
            get;
            private set;
        }

        public NameValueCollection Form
        {
            get;
            private set;
        }

        public IRequestCookieCollection Cookies
        {
            get;
            private set;
        }

        private NameValueCollection ParseDelimited(string s)
        {
            var nvc = new NameValueCollection();
            if (s == null)
            {
                return nvc;
            }

            foreach (var pair in s.Split('&'))
            {
                var kvp = pair.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (kvp.Length == 0)
                {
                    continue;
                }

                string key = kvp[0].Trim();
                if (String.IsNullOrEmpty(key))
                {
                    continue;
                }
                string value = kvp.Length > 1 ? kvp[1].Trim() : null;
                nvc[key] = UrlDecode(value);
            }

            return nvc;
        }

        private static string UrlDecode(string url)
        {
            if (url == null)
            {
                return null;
            }

            // HACK: Uri.UnescapeDataString doesn't seem to handle +
            // TODO: Copy impl from System.Web.HttpUtility.UrlDecode
            return Uri.UnescapeDataString(url).Replace("+", " ");
        }
    }
}
