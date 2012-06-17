using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Gate;
using SignalR.Hosting.Common;

namespace SignalR.Hosting.Owin
{
    public class OwinRequest : IRequest
    {
        private readonly CookieManager _cookies;

        public OwinRequest(IDictionary<string, object> environment, string body)
        {
            var env = new Gate.Environment(environment);
            var headers = env.Headers;

            Url = BuildUrl(env);
            Headers = new NameValueCollection();
            // TODO: Fill this up
            ServerVariables = new NameValueCollection();

            foreach (var pair in headers)
            {
                foreach (var value in pair.Value)
                {
                    Headers.Add(pair.Key, value);
                }
            }

            _cookies = new CookieManager();
            QueryString = HttpUtility.ParseDelimited(env.QueryString);
            Form = HttpUtility.ParseDelimited(body);
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
            get;
            private set;
        }

        public NameValueCollection Headers
        {
            get;
            private set;
        }

        public NameValueCollection ServerVariables
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

        public IPrincipal User
        {
            get
            {
                return Thread.CurrentPrincipal;
            }
        }

        /// <summary>
        /// Based on http://owin.org/spec/owin-1.0.0draft5.html#URIReconstruction
        /// </summary>
        private Uri BuildUrl(Gate.Environment env)
        {
            string url = env.Scheme + "://" + env.Headers.GetHeader("Host") + env.PathBase + env.Path;

            if (!String.IsNullOrEmpty(env.QueryString))
            {
                url += "?" + env.QueryString;
            }

            return new Uri(url);
        }

        public void AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {

        }
    }
}
