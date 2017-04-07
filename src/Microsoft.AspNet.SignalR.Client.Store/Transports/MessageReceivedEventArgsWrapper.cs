// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
