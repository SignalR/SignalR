// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public class HubDispatcherMiddleware : OwinMiddleware
    {
        private readonly HubConfiguration _configuration;

        public HubDispatcherMiddleware(OwinMiddleware next, HubConfiguration configuration)
            : base(next)
        {
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

            var dispatcher = new HubDispatcher(_configuration);

            dispatcher.Initialize(_configuration.Resolver);

            return dispatcher.ProcessRequest(context.Environment);
        }
    }
}
