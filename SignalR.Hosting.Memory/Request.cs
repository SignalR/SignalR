using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting.Common;
using IClientRequest = SignalR.Client.Http.IRequest;

namespace SignalR.Hosting.Memory
{
    public class Request : IClientRequest, IRequest
    {
        private readonly CancellationTokenSource _clientTokenSource;

        public Request(Uri uri, CancellationTokenSource clientTokenSource, Dictionary<string, string> postData, IPrincipal user)
        {
            Url = uri;
            _clientTokenSource = clientTokenSource;
            User = user;
            Form = new NameValueCollection();
            Headers = new NameValueCollection();
            ServerVariables = new NameValueCollection();
            QueryString = HttpUtility.ParseDelimited(Url.Query.TrimStart('?'));

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

        public IWebProxy Proxy { get; set; }

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

        public NameValueCollection ServerVariables
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

        public IPrincipal User
        {
            get;
            private set;
        }

        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            // TODO: Add support
            throw new NotSupportedException();
        }
    }
}
