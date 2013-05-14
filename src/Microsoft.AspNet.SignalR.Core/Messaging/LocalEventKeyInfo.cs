// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class LocalEventKeyInfo
    {
        public LocalEventKeyInfo(ulong id, MessageStore<Message> store)
        {
            Id = id;
            MessageStore = store;
        }

        public ulong Id { get; private set; }
        public MessageStore<Message> MessageStore { get; private set; }
    }
}
