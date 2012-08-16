using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using Owin;

namespace SignalR.Server
{
    public class ServerRequest : IRequest
    {
        readonly CallParameters _call;

        public ServerRequest(CallParameters call)
        {
            _call = call;
        }

        public Uri Url { get; private set; }
        public NameValueCollection QueryString { get; private set; }
        public NameValueCollection Headers { get; private set; }
        public NameValueCollection ServerVariables { get; private set; }
        public NameValueCollection Form { get; private set; }
        public IRequestCookieCollection Cookies { get; private set; }
        public IPrincipal User { get; private set; }
        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            throw new NotImplementedException();
        }
    }
}
