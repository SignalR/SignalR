// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Stress
{
    public class Subscriber : ISubscriber
    {
        public Subscriber(string id, IList<string> eventKeys)
        {
            Identity = id;
            EventKeys = new List<string>(eventKeys);
        }

        public IList<string> EventKeys
        {
            get;
            set;
        }

        event Action<ISubscriber, string> ISubscriber.EventKeyAdded
        {
            add
            {
            }
            remove
            {
            }
        }

        event Action<ISubscriber, string> ISubscriber.EventKeyRemoved
        {
            add
            {
            }
            remove
            {
            }
        }

        public Action<TextWriter> WriteCursor { get; set; }

        public Subscription Subscription { get; set; }

        public string Identity
        {
            get;
            private set;
        }
    }
}
