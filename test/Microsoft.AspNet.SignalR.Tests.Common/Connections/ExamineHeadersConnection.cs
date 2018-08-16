// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class ExamineHeadersConnection : PersistentConnection
    {
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            string refererHeader = request.Headers[System.Net.HttpRequestHeader.Referer.ToString()];
            string testHeader = request.Headers["test-header"];
            string userAgentHeader = request.Headers["User-Agent"];

            return Connection.Send(connectionId, new
            {
                refererHeader = refererHeader,
                testHeader = testHeader,
                userAgentHeader = userAgentHeader
            });
        }
    }
}
