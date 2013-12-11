// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Hosting
{
    public class HostContext
    {
        // Exposed to user code
        public IRequest Request { get; private set; } 

        public IResponse Response { get; private set; }

        // Owin environment dictionary
        public IDictionary<string, object> Environment { get; private set; }

        public HostContext(IRequest request, IResponse response)
        {
            Request = request;
            Response = response;

            Environment = new Dictionary<string, object>();
        }

        public HostContext(IDictionary<string, object> environment)
        {
            Request = new ServerRequest(environment);
            Response = new ServerResponse(environment);

            Environment = environment;
        }
    }
}
