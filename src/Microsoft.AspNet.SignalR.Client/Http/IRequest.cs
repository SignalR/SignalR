// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
#if (NET4 || NET45)
using System.Security.Cryptography.X509Certificates;
#endif

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// The http request
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// The user agent for this request.
        /// </summary>
        string UserAgent { get; set; }

        /// <summary>
        /// The credentials for this request.
        /// </summary>
        ICredentials Credentials { get; set; }

        /// <summary>
        /// The cookies for this request.
        /// </summary>
        CookieContainer CookieContainer { get; set; }

#if !SILVERLIGHT
        /// <summary>
        /// The proxy information for this request.
        /// </summary>
        IWebProxy Proxy { get; set; }
#endif

        /// <summary>
        /// The accept header for this request.
        /// </summary>
        string Accept { get; set; }

        /// <summary>
        /// Aborts the request.
        /// </summary>
        void Abort();

        /// <summary>
        /// Set Request Headers
        /// </summary>
        /// <param name="headers">request headers</param>
        void SetRequestHeaders(IDictionary<string, string> headers);

#if (NET4 || NET45)
        /// <summary>
        /// Sets client certificates
        /// </summary>
        /// <param name="certificates">client certificates</param>
        void AddClientCerts(X509CertificateCollection certificates);
#endif
    }
}
