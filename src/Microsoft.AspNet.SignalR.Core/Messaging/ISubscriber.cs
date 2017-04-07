// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public interface ISubscriber
    {
        IList<string> EventKeys { get; }

        Action<TextWriter> WriteCursor { get; set; }

        string Identity { get; }

        event Action<ISubscriber, string> EventKeyAdded;

        event Action<ISubscriber, string> EventKeyRemoved;

        Subscription Subscription { get; set; }
    }
}
