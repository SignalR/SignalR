// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin
{
    public class ServerRequest : IRequest
    {
        private INameValueCollection _queryString;
        private INameValueCollection _headers;
        private IDictionary<string, Cookie> _cookies;

        private readonly OwinRequest _request;

        private static Func<object, INameValueCollection> _queryStringGetter = queryString => new ReadableStringCollectionWrapper((IReadableStringCollection)queryString);
        private static Func<object, INameValueCollection> _headersGetter = headers => new ReadableStringCollectionWrapper((IHeaderDictionary)headers);
        private static Func<object, IDictionary<string, Cookie>> _cookiesGetter = cookies => GetCookies((RequestCookieCollection)cookies);

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
                return (_request.PathBase + _request.Path).Value;
            }
        }

        public INameValueCollection QueryString
        {
            get
            {
                return EnsureInitialized(ref _queryString, _queryStringGetter, _request.Query);
            }
        }

        public INameValueCollection Headers
        {
            get
            {
                return EnsureInitialized(ref _headers, _headersGetter, _request.Headers);
            }
        }

        public IDictionary<string, Cookie> Cookies
        {
            get
            {
                return EnsureInitialized(ref _cookies, _cookiesGetter, _request.Cookies);
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

        public async Task<INameValueCollection> ReadForm()
        {
            IFormCollection form = await _request.ReadFormAsync();
            return new ReadableStringCollectionWrapper(form);
        }

        private static IDictionary<string, Cookie> GetCookies(RequestCookieCollection requestCookies)
        {
            var cookies = new Dictionary<string, Cookie>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in requestCookies)
            {
                if (!cookies.ContainsKey(kv.Key))
                {
                    cookies.Add(kv.Key, new Cookie(kv.Key, kv.Value));
                }
            }
            return cookies;
        }

        // Support factory state in order to cache delegates
        private static T EnsureInitialized<T>(ref T target, Func<object, T> valueFactory, object state) where T : class
        {
            if (Volatile.Read<T>(ref target) != null)
            {
                return target;
            }

            T value = valueFactory(state);
            if (value == null)
            {
                throw new InvalidOperationException(); // todo
            }
            Interlocked.CompareExchange(ref target, value, null);
            return target;
        }
    }
}
