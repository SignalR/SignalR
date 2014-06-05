// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    internal class MessageReceivedEventArgsWrapper : IWebSocketResponse
    {
        private readonly MessageWebSocketMessageReceivedEventArgs _messageReceivedEventArgs;

        public MessageReceivedEventArgsWrapper(MessageWebSocketMessageReceivedEventArgs messageReceivedEventArgs)
        {
            _messageReceivedEventArgs = messageReceivedEventArgs;
        }

        public IDataReader GetDataReader()
        {
            return _messageReceivedEventArgs.GetDataReader();
        }
    }
}
