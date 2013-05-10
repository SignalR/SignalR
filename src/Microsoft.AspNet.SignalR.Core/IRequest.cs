// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR
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
        /// The local path part of the url
        /// </summary>
        string LocalPath { get; }
        
        /// <summary>
        /// Gets the querystring for this request.
        /// </summary>
        NameValueCollection QueryString { get; }

        /// <summary>
        /// Gets the headers for this request.
        /// </summary>
        NameValueCollection Headers { get; }

        /// <summary>
        /// Gets the cookies for this request.
        /// </summary>
        IDictionary<string, Cookie> Cookies { get; }

        /// <summary>
        /// Gets security information for the current HTTP request.
        /// </summary>
        IPrincipal User { get; }

        /// <summary>
        /// Gets the owin enviornment
        /// </summary>
        IDictionary<string, object> Environment { get; }

        /// <summary>
        /// Reads the form of the http request
        /// </summary>
        /// <returns></returns>
        Task<NameValueCollection> ReadForm();
    }
}
