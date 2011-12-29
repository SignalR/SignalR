using System.Collections.Specialized;
using System.Security.Principal;

namespace SignalR.Hubs
{
    public class HubContext
    {
        /// <summary>
        /// Gets the connection id of the calling client.
        /// </summary>
        public string ConnectionId { get; private set; }

        /// <summary>
        /// Gets the cookies for the request
        /// </summary>
        public NameValueCollection Cookies { get; private set; }

        public IPrincipal User { get; private set; }

        public HubContext(string connectionId, NameValueCollection cookies, IPrincipal user)
        {
            ConnectionId = connectionId;
            Cookies = cookies;
            User = user;
        }
    }
}
