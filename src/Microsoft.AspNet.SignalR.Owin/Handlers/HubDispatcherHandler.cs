// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Owin.Handlers
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class HubDispatcherHandler
    {
        private readonly AppFunc _next;
        private readonly string _path;
        private readonly HubConfiguration _configuration;

        public HubDispatcherHandler(AppFunc next, string path, HubConfiguration configuration)
        {
            _next = next;
            _path = path;
            _configuration = configuration;
        }

        public Task Invoke(IDictionary<string, object> environment)
        {
            var path = environment.Get<string>(OwinConstants.RequestPath);
            if (path == null || !path.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
            {
                return _next(environment);
            }

            var pathBase = environment.Get<string>(OwinConstants.RequestPathBase);
            var dispatcher = new HubDispatcher(pathBase + _path, _configuration.EnableJavaScriptProxies);

            var handler = new CallHandler(_configuration, dispatcher);
            return handler.Invoke(environment);
        }
    }
}
