﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public enum MessageBusType
    {
        Default,
        Fake,
        FakeMultiStream,
        SqlServer,
        ServiceBus,
        Redis
    }
}
