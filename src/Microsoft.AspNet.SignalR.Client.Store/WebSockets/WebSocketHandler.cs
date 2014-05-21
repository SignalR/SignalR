// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.WebSockets
{
    public abstract class WebSocketHandler
    {
        private readonly int? _maxIncomingMessageSize;

        protected WebSocketHandler(int? maxIncomingMessageSize)
        {
            _maxIncomingMessageSize = maxIncomingMessageSize;
        }

        public abstract void OnOpen();

        public abstract void OnMessage(string message);

        public abstract void OnMessage(byte[] message);

        public abstract void OnError();

        public abstract void OnClose();

        // Sends a text message to the client
        public virtual Task Send(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            throw new NotImplementedException();
        }

        public virtual Task CloseAsync()
        {
            throw new NotImplementedException();
        }

        public int? MaxIncomingMessageSize
        {
            get { return _maxIncomingMessageSize; }
        }

        public Exception Error { get; set; }
    }
}
