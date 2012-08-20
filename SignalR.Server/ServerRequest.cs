using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using Owin;
using SignalR.Server.Utils;

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
                var uriBuilder = new UriBuilder(_req.Scheme, _req.Host, _req.Call.Port(), _req.PathBase + _req.Path);
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
                var remoteIp = _req.Call.RemoteIp();
                if (!string.IsNullOrEmpty(remoteIp))
                {
                    collection["REMOTE_ADDR"] = remoteIp;
                }
                return collection;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                var collection = new NameValueCollection();
#if true
                var form = _req.ReadForm();
                foreach (var kv in form)
                {
                    collection.Add(kv.Key, kv.Value);
                }
#else
                var readText = _req.ReadText();
                foreach (var kv in ParamDictionary.Parse(readText))
                {
                    collection.Add(kv.Key, kv.Value);
                }
#endif
                return collection;
            }
        }

        public IRequestCookieCollection Cookies
        {
            get { return new ServerCookieCollection(_req); }
        }

        public IPrincipal User
        {
            get { return _req.Call.Get<IPrincipal>("server.User"); }
        }

        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            throw new NotImplementedException();
        }
    }
}
