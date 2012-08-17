using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using Gate.Utils;
using Owin;
using SignalR.Server.Utils;

namespace SignalR.Server
{
    public class ServerRequest : IRequest
    {
        readonly CallParameters _call;
        readonly Gate.Request _req;

        public ServerRequest(CallParameters call)
        {
            _call = call;
            _req = new Gate.Request(_call);
        }

        public Uri Url
        {
            get
            {
                var uriBuilder = new UriBuilder(_call.Scheme(), _call.Host(), _call.Port(), _call.PathBase() + _call.Path());
                if (!string.IsNullOrEmpty(_call.QueryString()))
                {
                    uriBuilder.Query = _call.QueryString();
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
                var remoteIp = _call.RemoteIp();
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
                var readText = _req.ReadText();
                var collection = new NameValueCollection();
                foreach (var kv in ParamDictionary.Parse(readText))
                {
                    collection.Add(kv.Key, kv.Value);
                }
                return collection;
            }
        }

        public IRequestCookieCollection Cookies
        {
            get { return new ServerCookieCollection(_req); }
        }

        public IPrincipal User
        {
            get { return _call.Get<IPrincipal>("server.User"); }
        }

        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            throw new NotImplementedException();
        }
    }
}
