// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public interface IInvalidClientContract
    {
        // Methods must return void or Task, so this is invalid
        bool Echo(string message);
        Task Ping();
    }
}
