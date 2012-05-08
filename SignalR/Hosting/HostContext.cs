using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace SignalR.Hosting
{
    public class HostContext
    {
        public IRequest Request { get; private set; }
        public IResponse Response { get; private set; }
        public IDictionary<string, object> Items { get; private set; }

        public HostContext(IRequest request, IResponse response)
        {
            Request = request;
            Response = response;
            Items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
