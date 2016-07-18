// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMapping
    {
        public ScaleoutMapping(ulong id, ScaleoutMessage message)
            : this(id, message, ListHelper<LocalEventKeyInfo>.Empty)
        {
        }

        public ScaleoutMapping(ulong id, ScaleoutMessage message, IList<LocalEventKeyInfo> localKeyInfo)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (localKeyInfo == null)
            {
                throw new ArgumentNullException("localKeyInfo");
            }

            Id = id;
            LocalKeyInfo = localKeyInfo;
            ServerCreationTime = message.ServerCreationTime;
        }

        public ulong Id { get; private set; }
        public IList<LocalEventKeyInfo> LocalKeyInfo { get; private set; }
        public DateTime ServerCreationTime { get; private set; }
    }
}
