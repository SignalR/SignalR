// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public class HubDispatcherMiddleware : SignalRMiddleware
    {
        private readonly HubConfiguration _configuration;

        public HubDispatcherMiddleware(OwinMiddleware next, string path, HubConfiguration configuration)
            : base(next, path, configuration)
        {
            _configuration = configuration;
        }

        protected override Task ProcessRequest(IOwinContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var dispatcher = new HubDispatcher(_configuration);

            dispatcher.Initialize(_configuration.Resolver);

            return dispatcher.ProcessRequest(context.Environment);
        }
    }
}
