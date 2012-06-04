using System.Collections.Specialized;
using System.Security.Principal;

namespace SignalR.Hubs
{
    public class HubCallerContext
    {
        /// <summary>
        /// Gets the connection id of the calling client.
        /// </summary>
        public string ConnectionId { get; private set; }

        /// <summary>
        /// Gets the cookies for the request
        /// </summary>
        public IRequestCookieCollection RequestCookies { get; private set; }

        /// <summary>
        /// Gets the headers for the request
        /// </summary>
        public NameValueCollection Headers { get; private set; }

        /// <summary>
        /// Gets the querystring for the request
        /// </summary>
        public NameValueCollection QueryString { get; private set; }

        public IPrincipal User { get; private set; }

        public HubCallerContext(IRequest request, string connectionId)
        {
            ConnectionId = connectionId;

            if (request != null)
            {
                RequestCookies = request.Cookies;
                Headers = request.Headers;
                QueryString = request.QueryString;
                User = request.User;
            }
        }
    }
}
