// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class Command
    {
        public Command()
        {
            Id = Guid.NewGuid().ToString();
        }
        
        public bool WaitForAck { get; set; }
        public string Id { get; private set; }
        public CommandType CommandType { get; set; }
        public string Value { get; set; }
    }
}
