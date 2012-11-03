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
        private readonly AppFunc _app;
        private readonly string _path;
        private readonly IDependencyResolver _resolver;

        public HubDispatcherHandler(AppFunc app, IDependencyResolver resolver)
            : this(app, String.Empty, resolver)
        {
        }

        public HubDispatcherHandler(AppFunc app, string path, IDependencyResolver resolver)
        {
            _app = app;
            _path = path;
            _resolver = resolver;
        }

        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            return env.TryGetValue(key, out value) ? (T)value : default(T);
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var path = Get<string>(env, OwinConstants.RequestPath);
            if (path == null || !path.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
            {
                return _app.Invoke(env);
            }

            var pathBase = Get<string>(env, OwinConstants.RequestPathBase);
            var dispatcher = new HubDispatcher(pathBase + _path);

            var handler = new CallHandler(_resolver, dispatcher);
            return handler.Invoke(env);
        }
    }
}
