// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    /// Represents a change in the connection state.
    /// </summary>
    public class StateChange
    {
        /// <summary>
        /// Creates a new stance of <see cref="StateChange"/>.
        /// </summary>
        /// <param name="oldState">The old state of the connection.</param>
        /// <param name="newState">The new state of the connection.</param>
        public StateChange(ConnectionState oldState, ConnectionState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        /// <summary>
        /// Gets the old state of the connection.
        /// </summary>
        public ConnectionState OldState { get; private set; }

        /// <summary>
        /// Gets the new state of the connection.
        /// </summary>
        public ConnectionState NewState { get; private set; }
    }
}
