using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SignalR.Hubs {
    public class HubContext {
        /// <summary>
        /// Gets the client id of the calling client.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Gets the cookies for the request
        /// </summary>
        public HttpCookieCollection Cookies { get; private set; }

        public HubContext(string clientId, HttpCookieCollection cookies) {
            ClientId = clientId;
            Cookies = cookies;
        }
    }
}
