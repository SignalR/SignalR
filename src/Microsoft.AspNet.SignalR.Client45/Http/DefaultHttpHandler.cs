﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if !NETFX_CORE && !PORTABLE
using System.Security.Cryptography.X509Certificates;
#endif
using System.Net.Http;

namespace Microsoft.AspNet.SignalR.Client.Http
{
#if !NETFX_CORE && !PORTABLE && !__ANDROID__ && !IOS && !NETSTANDARD
    public class DefaultHttpHandler : WebRequestHandler
#else
    public class DefaultHttpHandler : HttpClientHandler
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
