using System;

namespace SignalR
{
    // Represents the result of a call to MessageStore<T>.GetMessages.
    public struct MessageStoreResult<T> where T : class
    {
        // The first message ID in the result set. Messages in the result set have sequentually increasing IDs.
        // If FirstMessageId = 20 and Messages.Length = 4, then the messages have IDs { 20, 21, 22, 23 }.
        public readonly ulong FirstMessageId;

        // If this is true, the backing MessageStore contains more messages, and the client should call GetMessages again.
        public readonly bool HasMoreData;

        // The actual result set. May be empty.
        public readonly T[] Messages;

        public MessageStoreResult(ulong firstMessageId, T[] messages, bool hasMoreData)
        {
            FirstMessageId = firstMessageId;
            Messages = messages;
            HasMoreData = hasMoreData;
        }
    }
}
