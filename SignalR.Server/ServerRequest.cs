using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;

namespace SignalR.Server
{
    class ServerRequest : IRequest
    {
        readonly Gate.Request _req;

        internal ServerRequest(Gate.Request req)
        {
            _req = req;
        }

        public Uri Url
        {
            get
            {
                var uriBuilder = new UriBuilder(_req.Scheme, _req.Host, _req.Port, _req.PathBase + _req.Path);
                if (!string.IsNullOrEmpty(_req.QueryString))
                {
                    uriBuilder.Query = _req.QueryString;
                }
                return uriBuilder.Uri;
            }
        }


        public NameValueCollection QueryString
        {
            get
            {
                var collection = new NameValueCollection();
                foreach (var kv in _req.Query)
                {
                    collection.Add(kv.Key, kv.Value);
                }
                return collection;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                var collection = new NameValueCollection();
                foreach (var kv in _req.Headers)
                {
                    if (kv.Value != null)
                    {
                        for (var index = 0; index != kv.Value.Length; ++index)
                        {
                            collection.Add(kv.Key, kv.Value[index]);
                        }
                    }
                }
                return collection;
            }
        }

        public NameValueCollection ServerVariables
        {
            get
            {
                var collection = new NameValueCollection();
                var remoteIpAddress = _req.Environment.Get<string>("server.RemoteIpAddress");
                if (!string.IsNullOrEmpty(remoteIpAddress))
                {
                    collection["REMOTE_ADDR"] = remoteIpAddress;
                }
                return collection;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                var collection = new NameValueCollection();
                var form = _req.ReadForm();
                foreach (var kv in form)
                {
                    collection.Add(kv.Key, kv.Value);
                }
                return collection;
            }
        }

        public IRequestCookieCollection Cookies
        {
            get { return new ServerRequestCookieCollection(_req); }
        }

        public IPrincipal User
        {
            get { return _req.Environment.Get<IPrincipal>("server.User"); }
        }

        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            throw new NotImplementedException();
        }
    }
}
