// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting.Common;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    public class Request : IClientRequest, IRequest
    {
        private readonly Action _abort;

        public Request(Uri uri, Action abort, Dictionary<string, string> postData, IPrincipal user)
        {
            Url = uri;
            _abort = abort;
            User = user;
            Form = new NameValueCollection();
            Headers = new NameValueCollection();
            ServerVariables = new NameValueCollection();
            QueryString = HttpUtility.ParseDelimited(Url.Query.TrimStart('?'));
            Cookies = new RequestCookieCollection();

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
            _abort();
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

        private class RequestCookieCollection : IRequestCookieCollection
        {
            public Cookie this[string name]
            {
                get { return null; }
            }

            public int Count
            {
                get { return 0; }
            }
        }
    }
}
