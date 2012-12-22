// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class LocalEventKeyInfo
    {
        public LocalEventKeyInfo()
        {
            MinLocal = Int32.MaxValue;
        }

        public MessageStore<Message> Store { get; set; }
        public ulong MinLocal { get; set; }
        public int Count { get; set; }
    }
}
