// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin
{
    public class ServerRequest : IRequest
    {
        private NameValueCollection _queryString;
        private NameValueCollection _headers;
        private IDictionary<string, Cookie> _cookies;

        private readonly OwinRequest _request;

        public ServerRequest(IDictionary<string, object> environment)
        {
            _request = new OwinRequest(environment);
        }

        public Uri Url
        {
            get
            {
                return _request.Uri;
            }
        }

        public string LocalPath
        {
            get
            {
                return _request.PathBase + _request.Path;
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
                        foreach (var kv in _request.GetQuery())
                        {
                            collection.Add(kv.Key, kv.Value[0]);
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

        public IDictionary<string, Cookie> Cookies
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _cookies, () =>
                    {
                        var cookies = new Dictionary<string, Cookie>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kv in _request.GetCookies())
                        {
                            if (!cookies.ContainsKey(kv.Key))
                            {
                                cookies.Add(kv.Key, new Cookie(kv.Key, kv.Value));
                            }
                        }
                        return cookies;
                    });
            }
        }

        public IPrincipal User
        {
            get { return _request.User; }
        }

        public IDictionary<string, object> Environment
        {
            get
            {
                return _request.Environment;
            }
        }

        private IDictionary<string, string[]> RequestHeaders
        {
            get { return Environment.Get<IDictionary<string, string[]>>(OwinConstants.RequestHeaders); }
        }
        
        public Task<NameValueCollection> ReadForm()
        {
            return _request.ReadForm();
        }
    }
}
