using System;
using System.Collections.Specialized;
using System.Web;
using SignalR.Abstractions;

namespace SignalR.AspNet
{
    public class AspNetRequest : IRequest
    {
        private static readonly Lazy<bool> _hasAcceptWebSocketRequest =
            new Lazy<bool>(() => typeof(HttpContextBase).GetMethod("AcceptWebSocketRequest") != null);

        private readonly HttpRequestBase _request;

        public AspNetRequest(HttpRequestBase request)
        {
            _request = request;
            Cookies = new NameValueCollection();
            foreach (string key in request.Cookies)
            {
                Cookies.Add(key, request.Cookies[key].Value);
            }
        }

        public string Path
        {
            get
            {
                return _request.Path;
            }
        }

        public string LocalPath
        {
            get
            {
                return _request.AppRelativeCurrentExecutionFilePath;
            }
        }

        public bool SupportsWebSockets
        {
            get
            {
                return _hasAcceptWebSocketRequest.Value;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                return _request.QueryString;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return _request.Headers;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                return _request.Form;
            }
        }

        public NameValueCollection Cookies
        {
            get;
            private set;
        }        
    }
}
