using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SignalR.Abstractions;

namespace SignalR.Owin
{
    public class OwinRequest : IRequest
    {
        public OwinRequest(IDictionary<string, object> environment, string body)
        {
            var env = new Gate.Environment(environment);
            var headers = env.Headers;

            Url = BuildUrl(env);
            Headers = new NameValueCollection();

            foreach (var pair in headers)
            {
                Headers.Add(pair.Key, pair.Value);
            }

            Cookies = new NameValueCollection();
            QueryString = ParseDelimited(env.QueryString);
            Form = ParseDelimited(body);
        }

        public NameValueCollection Cookies
        {
            get;
            private set;
        }

        public NameValueCollection Form
        {
            get;
            private set;
        }

        public NameValueCollection Headers
        {
            get;
            private set;
        }

        public NameValueCollection QueryString
        {
            get;
            private set;
        }

        public Uri Url
        {
            get;
            private set;
        }

        /// <summary>
        /// Based on http://owin.org/spec/owin-1.0.0draft5.html#URIReconstruction
        /// </summary>
        private Uri BuildUrl(Gate.Environment env)
        {
            string url = env.Scheme + "://" + env.Headers["Host"] + env.PathBase + env.Path;

            if (!String.IsNullOrEmpty(env.QueryString))
            {
                url += "?" + env.QueryString;
            }

            return new Uri(url);
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
