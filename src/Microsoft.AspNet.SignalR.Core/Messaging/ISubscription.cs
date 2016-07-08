// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public interface ISubscription
    {
        string Identity { get; }

        bool SetQueued();
        bool UnsetQueued();

        Task Work();
    }
}
