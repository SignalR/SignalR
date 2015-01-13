using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeWebSocket : IWebSocket, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();

        public void Close(ushort code, string reason)
        {
            _invocationManager.AddInvocation("Close", code, reason);
        }

        public event TypedEventHandler<IWebSocket, WebSocketClosedEventArgs> Closed
        {
            add { throw new NotImplementedException(); }
            remove { }
        }

        public IAsyncAction ConnectAsync(Uri uri)
        {
            throw new NotImplementedException();
        }

        public IOutputStream OutputStream { get; set; }

        public void SetRequestHeader(string headerName, string headerValue)
        {
            _invocationManager.AddInvocation("SetRequestHeader", headerName, headerValue);
        }

        public void Dispose()
        {
        }

        public void Verify(string methodName, List<object[]> expectedParameters)
        {
            _invocationManager.Verify(methodName, expectedParameters);
        }

        public IEnumerable<object[]> GetInvocations(string methodName)
        {
            return _invocationManager.GetInvocations(methodName);
        }

        void IFake.Setup<T>(string methodName, Func<T> behavior)
        {
            throw new NotImplementedException();
        }
    }
}