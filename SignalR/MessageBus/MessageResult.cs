using System.Collections.Generic;
using System.Linq;

namespace SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public struct MessageResult
    {
        private static readonly Message[] _emptyList = new Message[0];

        /// <summary>
        /// Gets an <see cref="IList{Message}"/> associated with the result.
        /// </summary>
        public Message[] Messages { get; private set; }

        /// <summary>
        /// Gets a cursor representing the caller state.
        /// </summary>
        public string LastMessageId { get; private set; }

        /// <summary>
        /// The amount of messages in the Messages array
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageResult"/> struct.
        /// </summary>
        /// <param name="lastMessageId">Gets a cursor representing the caller state.</param>
        public MessageResult(string lastMessageId)
            : this(_emptyList, lastMessageId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageResult"/> struct.
        /// </summary>
        /// <param name="messages">The list of messages associated with this <see cref="MessageResult"/>.</param>
        /// <param name="lastMessageId">Gets a cursor representing the caller state.</param>
        public MessageResult(IList<Message> messages, string lastMessageId)
            : this()
        {
            Messages = messages.ToArray();
            LastMessageId = lastMessageId;
            Count = Messages.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageResult"/> struct.
        /// </summary>
        /// <param name="messages">The array of messages associated with this <see cref="MessageResult"/>.</param>
        /// <param name="lastMessageId">Gets a cursor representing the caller state.</param>
        /// <param name="count">The amount of messages populated in the messages array.</param>
        public MessageResult(Message[] messages, string lastMessageId, int count)
            : this()
        {
            Messages = messages;
            LastMessageId = lastMessageId;
            Count = count;
        }
    }
}
