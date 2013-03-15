// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class LocalEventKeyInfo
    {
        public LocalEventKeyInfo(string key, ulong id, MessageStore<Message> store)
        {
            Key = key;
            Id = id;
            Store = store;
        }

        public string Key { get; private set; }
        public ulong Id { get; private set; }
        public MessageStore<Message> Store { get; private set; }
    }
}
