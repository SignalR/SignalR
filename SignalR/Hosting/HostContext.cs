using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace SignalR.Hosting
{
    public class HostContext
    {
        public IRequest Request { get; private set; }
        public IResponse Response { get; private set; }
        public IPrincipal User { get; private set; }
        public IDictionary<string, object> Items { get; private set; }

        public HostContext(IRequest request, IResponse response, IPrincipal user)
        {
            Request = request;
            Response = response;
            User = user;
            Items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
