// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class PreserializedJsonConnection : PersistentConnection
    {
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            var jsonBytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
            return Connection.Send(connectionId, jsonBytes);
        }
    }
}
