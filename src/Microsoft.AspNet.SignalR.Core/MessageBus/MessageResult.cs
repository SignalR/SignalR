// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// 
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Messages are never compared")]
    public struct MessageResult
    {
        private static readonly List<ArraySegment<Message>> _emptyList = new List<ArraySegment<Message>>();

        /// <summary>
        /// Gets an <see cref="IList{Message}"/> associated with the result.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an optimization to avoid allocations.")]
        public IList<ArraySegment<Message>> Messages { get; private set; }

        public int TotalCount { get; private set; }

        public bool Terminal { get; set; }

        /// <summary>
        /// Gets a cursor representing the caller state.
        /// </summary>
        public string LastMessageId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageResult"/> struct.
        /// </summary>
        /// <param name="lastMessageId">Gets a cursor representing the caller state.</param>
        public MessageResult(string lastMessageId) :
            this(_emptyList, lastMessageId, totalCount: 0)
        {
            Terminal = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageResult"/> struct.
        /// </summary>
        /// <param name="messages">The array of messages associated with this <see cref="MessageResult"/>.</param>
        /// <param name="lastMessageId">Gets a cursor representing the caller state.</param>
        /// <param name="totalCount">The amount of messages populated in the messages array.</param>
        public MessageResult(IList<ArraySegment<Message>> messages, string lastMessageId, int totalCount)
            : this()
        {
            Messages = messages;
            LastMessageId = lastMessageId;
            TotalCount = totalCount;
        }
    }
}
