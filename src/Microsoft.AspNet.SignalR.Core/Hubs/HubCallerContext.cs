// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class HubCallerContext
    {
        /// <summary>
        /// Gets the connection id of the calling client.
        /// </summary>
        public virtual string ConnectionId { get; private set; }

        /// <summary>
        /// Gets the cookies for the request.
        /// </summary>
        public virtual IDictionary<string, Cookie> RequestCookies
        {
            get
            {
                return Request.Cookies;
            }
        }

        /// <summary>
        /// Gets the headers for the request.
        /// </summary>
        public virtual INameValueCollection Headers
        {
            get
            {
                return Request.Headers;
            }
        }

        /// <summary>
        /// Gets the querystring for the request.
        /// </summary>
        public virtual INameValueCollection QueryString
        {
            get
            {
                return Request.QueryString;
            }
        }

        /// <summary>
        /// Gets the <see cref="IPrincipal"/> for the request.
        /// </summary>
        public virtual IPrincipal User
        {
            get
            {
                return Request.User;
            }
        }

        /// <summary>
        /// Gets the <see cref="IRequest"/> for the current HTTP request.
        /// </summary>
        public virtual IRequest Request { get; private set; }

        /// <summary>
        /// This constructor is only intended to enable mocking of the class. Use of this constructor 
        /// for other purposes may result in unexpected behavior.   
        /// </summary>
        protected HubCallerContext() { }

        public HubCallerContext(IRequest request, string connectionId)
        {
            ConnectionId = connectionId;
            Request = request;
        }
    }
}
