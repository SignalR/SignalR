// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public interface IAckHandler
    {
        Task CreateAck(string id);

        bool TriggerAck(string id);
    }
}
