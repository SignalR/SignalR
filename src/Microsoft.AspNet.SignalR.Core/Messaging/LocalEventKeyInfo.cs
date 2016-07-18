// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class LocalEventKeyInfo
    {
        private readonly WeakReference _storeReference;

        public LocalEventKeyInfo(string key, ulong id, MessageStore<Message> store)
        {
            // Don't hold onto MessageStores that would otherwise be GC'd
            _storeReference = new WeakReference(store);
            Key = key;
            Id = id;
        }

        public string Key { get; private set; }
        public ulong Id { get; private set; }
        public MessageStore<Message> MessageStore
        {
            get
            {
                return _storeReference.Target as MessageStore<Message>;
            }
        }
    }
}
