// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hosting
{
    /// <summary>
    /// Represents a web socket.
    /// </summary>
    public interface IWebSocket
    {
        /// <summary>
        /// Invoked when data is sent over the websocket
        /// </summary>
        Action<string> OnMessage { get; set; }

        /// <summary>
        /// Invoked when the websocket gracefully closes
        /// </summary>
        Action<bool> OnClose { get; set; }

        /// <summary>
        /// Invoked when there is an error
        /// </summary>
        Action<Exception> OnError { get; set; }

        /// <summary>
        /// Sends data over the websocket.
        /// </summary>
        /// <param name="value">The value to send.</param>
        /// <returns>A <see cref="Task"/> that represents the send is complete.</returns>
        Task Send(string value);
    }
}
