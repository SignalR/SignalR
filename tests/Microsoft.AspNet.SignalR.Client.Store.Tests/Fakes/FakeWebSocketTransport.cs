using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeWebSocketTransport : WebSocketTransport, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();

        protected override Task OpenWebSocket(IWebSocket webSocket, Uri uri)
        {
            _invocationManager.AddInvocation("OpenWebSocket", webSocket, uri);

            return _invocationManager.GetReturnValue<Task>("OpenWebSocket");
        }

        protected override void OnStartFailed()
        {
            _invocationManager.AddInvocation("OnStartFailed");
        }

        public void Setup<T>(string methodName, Func<T> behavior)
        {
            _invocationManager.AddSetup(methodName, behavior);
        }

        void IFake.Verify(string methodName, List<object[]> expectedParameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object[]> GetInvocations(string methodName)
        {
            return _invocationManager.GetInvocations(methodName);
        }
    }
}
