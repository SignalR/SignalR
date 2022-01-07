// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class MyBadConnection : PersistentConnection
    {
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            var orig = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Should throw 404
            try
            {
                using (HttpWebRequest.Create("https://httpstat.us/404").GetResponse()) { }
            }
            finally
            {
                ServicePointManager.SecurityProtocol = orig;
            }

            return base.OnConnected(request, connectionId);
        }
    }
}
