// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public class PersistentConnectionMiddleware : OwinMiddleware
    {
        private readonly Type _connectionType;
        private readonly ConnectionConfiguration _configuration;

        public PersistentConnectionMiddleware(OwinMiddleware next,
                                              Type connectionType,
                                              ConnectionConfiguration configuration)
            : base(next)
        {
            _connectionType = connectionType;
            _configuration = configuration;
        }

        public override Task Invoke(IOwinContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (JsonUtility.TryRejectJSONPRequest(_configuration, context))
            {
                return TaskAsyncHelper.Empty;
            }

            var connectionFactory = new PersistentConnectionFactory(_configuration.Resolver);
            PersistentConnection connection = connectionFactory.CreateInstance(_connectionType);

            connection.Initialize(_configuration.Resolver);

            return connection.ProcessRequest(context.Environment);
        }
    }
}
