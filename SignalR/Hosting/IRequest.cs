using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;

namespace SignalR.Hosting
{
    public interface IRequest
    {
        Uri Url { get; }
        NameValueCollection QueryString { get; }
        NameValueCollection Headers { get; }
        NameValueCollection Form { get; }
        IRequestCookieCollection Cookies { get; }
        IPrincipal User { get; }

        void AcceptWebSocketRequest(Func<IWebSocket, Task> callback);
    }
}
