// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.SignalR.Stress
{
    public class Subscriber : ISubscriber
    {
        public Subscriber(string id, IEnumerable<string> eventKeys)
        {
            Identity = id;
            EventKeys = eventKeys;
        }

        public IEnumerable<string> EventKeys
        {
            get;
            set;
        }

        event Action<string> ISubscriber.EventKeyAdded
        {
            add
            {
            }
            remove
            {
            }
        }

        event Action<string> ISubscriber.EventKeyRemoved
        {
            add
            {
            }
            remove
            {
            }
        }

        public Func<string> GetCursor { get; set; }

        public string Identity
        {
            get;
            private set;
        }
    }
}
