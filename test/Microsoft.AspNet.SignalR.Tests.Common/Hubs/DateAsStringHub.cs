// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class TypeWithDateAsString
    {
        public string DateAsString { get; set; }
    }

    public class DateAsStringHub : Hub
    {
        public TypeWithDateAsString Invoke(TypeWithDateAsString value)
        {
            Clients.Caller.Callback(value);
            return value;
        }
    }
}
