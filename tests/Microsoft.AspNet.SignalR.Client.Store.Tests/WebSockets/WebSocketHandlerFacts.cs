

using Microsoft.AspNet.SignalR.WebSockets;
using System;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.WebSockets
{
    public class WebSocketHandlerFacts
    {
        [Fact]
        public void CanGetSetMaxIncomingMessageSize()
        {
            Assert.Equal(42, new WebSocketHandlerFake(42).MaxIncomingMessageSize);
        }

        #region WebSocketHandlerFake
        private class WebSocketHandlerFake : WebSocketHandler
        {
            public WebSocketHandlerFake(int? maxIncomingMessageSize)
                : base(maxIncomingMessageSize)
            {
            }

            public override void OnOpen()
            {
                throw new NotImplementedException();
            }

            public override void OnMessage(string message)
            {
                throw new NotImplementedException();
            }

            public override void OnMessage(byte[] message)
            {
                throw new NotImplementedException();
            }

            public override void OnError()
            {
                throw new NotImplementedException();
            }

            public override void OnClose()
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
