// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR
{
    // Represents the result of a call to MessageStore<T>.GetMessages.
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "This is never compared")]
    public struct MessageStoreResult<T> where T : class
    {
        // The first message ID in the result set. Messages in the result set have sequentually increasing IDs.
        // If FirstMessageId = 20 and Messages.Length = 4, then the messages have IDs { 20, 21, 22, 23 }.
        public readonly ulong FirstMessageId;

        // If this is true, the backing MessageStore contains more messages, and the client should call GetMessages again.
        public readonly bool HasMoreData;

        // The actual result set. May be empty.
        public readonly ArraySegment<T> Messages;

        public MessageStoreResult(ulong firstMessageId, ArraySegment<T> messages, bool hasMoreData)
        {
            FirstMessageId = firstMessageId;
            Messages = messages;
            HasMoreData = hasMoreData;
        }
    }
}
