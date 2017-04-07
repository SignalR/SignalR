// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class TestSubscriber : ISubscriber
    {
        public TestSubscriber(IList<string> keys)
        {
            EventKeys = new List<string>(keys);
            Identity = Guid.NewGuid().ToString();
        }

        public IList<string> EventKeys { get; private set; }

        public string Identity { get; private set; }

        public event Action<ISubscriber, string> EventKeyAdded;

        public event Action<ISubscriber, string> EventKeyRemoved;

        public Action<TextWriter> WriteCursor { get; set; }

        public Subscription Subscription { get; set; }

        public void AddEvent(string eventName)
        {
            if (EventKeyAdded != null)
            {
                EventKeyAdded(this, eventName);
            }
        }

        public void RemoveEvent(string eventName)
        {
            if (EventKeyRemoved != null)
            {
                EventKeyRemoved(this, eventName);
            }
        }
    }
}
