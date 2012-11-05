// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Owin.Handlers
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class PersistentConnectionHandler
    {
        private readonly AppFunc _app;
        private readonly string _path;
        private readonly Type _connectionType;
        private readonly IDependencyResolver _resolver;

        public PersistentConnectionHandler(AppFunc app, string path, Type connectionType, IDependencyResolver resolver)
        {
            _app = app;
            _path = path;
            _connectionType = connectionType;
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

            var connectionFactory = new PersistentConnectionFactory(_resolver);
            var connection = connectionFactory.CreateInstance(_connectionType);

            var handler = new CallHandler(_resolver, connection);
            return handler.Invoke(env);
        }
    }
}
