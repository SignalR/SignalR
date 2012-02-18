﻿using System.Collections.Specialized;
using System.Security.Principal;
using SignalR.Hosting;

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

        /// <summary>
        /// Gets the headers for the request
        /// </summary>
        public NameValueCollection Headers { get; private set; }

        /// <summary>
        /// Gets the querystring for the request
        /// </summary>
        public NameValueCollection QueryString { get; private set; }

        public IPrincipal User { get; private set; }

        public HubContext(HostContext context, string connectionId)
        {
            ConnectionId = connectionId;
            Cookies = context.Request.Cookies;
            Headers = context.Request.Headers;
            QueryString = context.Request.QueryString;
            User = context.User;
        }
    }
}
