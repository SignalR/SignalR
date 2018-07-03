// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD2_0

using System;
#if !NETFX_CORE && !PORTABLE
using System.Security.Cryptography.X509Certificates;
#endif
using System.Net.Http;

namespace Microsoft.AspNet.SignalR.Client.Http
{
#if NET45
    public class DefaultHttpHandler : WebRequestHandler
#elif NETSTANDARD2_0
    public class DefaultHttpHandler : HttpClientHandler
#else
#error Unsupported target framework.
#endif
    {
        private readonly IConnection _connection;

        public DefaultHttpHandler(IConnection connection)
        {
            if (connection != null)
            {
                _connection = connection;
            }
            else
            {
                throw new ArgumentNullException("connection");
            }

            Credentials = _connection.Credentials;
#if PORTABLE
            if (this.SupportsPreAuthenticate())
            {
                PreAuthenticate = true;
            }
#elif NET45 || NETSTANDARD
            PreAuthenticate = true;
#endif

            if (_connection.CookieContainer != null)
            {
                CookieContainer = _connection.CookieContainer;
            }

#if !PORTABLE
            if (_connection.Proxy != null)
            {
                Proxy = _connection.Proxy;
            }
#endif

#if (NET4 || NET45 || NETSTANDARD)
            foreach (X509Certificate cert in _connection.Certificates)
            {
                ClientCertificates.Add(cert);
            }
#endif
        }
    }
}

#elif NET40
// Not required on this framework.
#else 
#error Unsupported target framework.
#endif

