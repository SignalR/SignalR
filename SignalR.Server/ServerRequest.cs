using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Server.Infrastructure;

namespace SignalR.Server
{
    public partial class ServerRequest : IRequest
    {
        private static readonly char[] CommaSemicolon = new[] { ',', ';' };

        private Uri _url;
        private NameValueCollection _queryString;
        private NameValueCollection _headers;
        private NameValueCollection _serverVariables;
        private NameValueCollection _form;
        private bool _formInitialized;
        private object _formLock;
        private ServerRequestCookieCollection _cookies;

        public Uri Url
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _url, () =>
                    {
                        var uriBuilder = new UriBuilder(RequestScheme, RequestHost, RequestPort, RequestPathBase + RequestPath);
                        if (!String.IsNullOrEmpty(RequestQueryString))
                        {
                            uriBuilder.Query = RequestQueryString;
                        }
                        return uriBuilder.Uri;
                    });
            }
        }


        public NameValueCollection QueryString
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _queryString, () =>
                    {
                        var collection = new NameValueCollection();
                        foreach (var kv in ParamDictionary.ParseToEnumerable(RequestQueryString, null))
                        {
                            collection.Add(kv.Key, kv.Value);
                        }
                        return collection;
                    });
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _headers, () =>
                    {
                        var collection = new NameValueCollection();
                        foreach (var kv in RequestHeaders)
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
                    });
            }
        }

        public NameValueCollection ServerVariables
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _serverVariables, () =>
                    {
                        var collection = new NameValueCollection();
                        var remoteIpAddress = Get<string>(OwinConstants.RemoteIpAddress);
                        if (!String.IsNullOrEmpty(remoteIpAddress))
                        {
                            collection["REMOTE_ADDR"] = remoteIpAddress;
                        }
                        return collection;
                    });
            }
        }

        public NameValueCollection Form
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _form, ref _formInitialized, ref _formLock, () =>
                    {
                        var collection = new NameValueCollection();
                        foreach (var kv in ReadForm())
                        {
                            collection.Add(kv.Key, kv.Value);
                        }
                        return collection;
                    });
            }
        }


        public IRequestCookieCollection Cookies
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _cookies, () =>
                    {
                        IDictionary<string, Cookie> cookies = new Dictionary<string, Cookie>();
                        var text = RequestHeaders.GetHeader("Cookie");
                        foreach (var kv in ParamDictionary.ParseToEnumerable(text, CommaSemicolon))
                        {
                            if (!cookies.ContainsKey(kv.Key))
                            {
                                cookies.Add(kv.Key, new Cookie(kv.Key, kv.Value));
                            }
                        }
                        return new ServerRequestCookieCollection(cookies);
                    });
            }
        }

        public IPrincipal User
        {
            get { return Get<IPrincipal>(OwinConstants.User); }
        }

        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            var serverRequestWebSocket = new ServerRequestWebSocket(callback);
            _env[OwinConstants.ResponseStatusCode] = 101;
            _env[OwinConstants.WebSocketFunc] = (Func<IDictionary<string, object>, Task>)serverRequestWebSocket.Invoke;
            return TaskAsyncHelper.Empty;
        }
    }
}
