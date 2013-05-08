// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    internal class Request : IClientRequest
    {
        private readonly IDictionary<string, string[]> _requestHeaders;
        private readonly Action _abort;

        public Request(IDictionary<string, object> env, Action abort)
        {
            _requestHeaders = Get<IDictionary<string, string[]>>(env, OwinConstants.RequestHeaders);
            _abort = abort;
        }

        public string UserAgent
        {
            get
            {
                return _requestHeaders.GetHeader("User-Agent");
            }
            set
            {
                _requestHeaders.SetHeader("User-Agent", value);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:No upstream or protected callers", Justification = "Not Implemented")]
        public ICredentials Credentials
        {
            get;
            set;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:No upstream or protected callers", Justification = "Not Implemented")]
        public CookieContainer CookieContainer
        {
            get;
            set;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:No upstream or protected callers", Justification = "Not Implemented")]
        public IWebProxy Proxy
        {
            get;
            set;
        }

        public string Accept
        {
            get
            {
                return _requestHeaders.GetHeader("Accept");
            }
            set
            {
                _requestHeaders.SetHeader("Accept", value);
            }
        }

        public void Abort()
        {
            Trace.TraceInformation("Abort()");
            _abort();
        }

        private static T Get<T>(IDictionary<string, object> environment, string key)
        {
            object value;
            return environment.TryGetValue(key, out value) ? (T)value : default(T);
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            foreach (KeyValuePair<string, string> headerEntry in headers)
            {
                _requestHeaders.SetHeader(headerEntry.Key, headerEntry.Value);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:No upstream or protected callers", Justification = "Not Implemented")]
        [SuppressMessage("Microsoft.Performance", "CA1822:Never used", Justification = "Not Implemented")]
        [SuppressMessage("Microsoft.Performance", "CA1801:Parameter not being used", Justification = "Not Implemented")]
        public void AddClientCerts(X509CertificateCollection certificates)
        {
        }
    }
}
