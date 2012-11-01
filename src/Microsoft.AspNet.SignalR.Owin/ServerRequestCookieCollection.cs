// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Owin
{
    public class ServerRequestCookieCollection : IRequestCookieCollection
    {
        private readonly IDictionary<string, Cookie> _cookies;

        public ServerRequestCookieCollection(IDictionary<string, Cookie> cookies)
        {
            _cookies = cookies;
        }

        public Cookie this[string name]
        {
            get
            {
                Cookie value;
                return _cookies.TryGetValue(name, out value) ? value : null;
            }
        }

        public int Count
        {
            get { return _cookies.Count; }
        }
    }
}
