using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// Represents a SignalR request
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// Gets the url for this request.
        /// </summary>
        Uri Url { get; }
        
        /// <summary>
        /// Gets the querystring for this request.
        /// </summary>
        NameValueCollection QueryString { get; }

        /// <summary>
        /// Gets the headers for this request.
        /// </summary>
        NameValueCollection Headers { get; }

        /// <summary>
        /// Gets the form for this request.
        /// </summary>
        NameValueCollection Form { get; }

        /// <summary>
        /// Gets the cookies for this request.
        /// </summary>
        IRequestCookieCollection Cookies { get; }

        /// <summary>
        /// Gets security information for the current HTTP request.
        /// </summary>
        IPrincipal User { get; }

        /// <summary>
        /// Accepts an websocket request using the specified user function.
        /// </summary>
        /// <param name="callback">The callback that fires when the websocket is ready.</param>
        void AcceptWebSocketRequest(Func<IWebSocket, Task> callback);
    }
}
