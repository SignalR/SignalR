using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeTransportHelper : TransportHelper, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();
        
        public override void ProcessResponse(IConnection connection, string response, out bool shouldReconnect, out bool disconnected,
            Action onInitialized)
        {
            shouldReconnect = disconnected = false;
            _invocationManager.AddInvocation("ProcessResponse", connection, response, onInitialized);
        }

        void IFake.Setup<T>(string methodName, Func<T> behavior)
        {
            throw new NotImplementedException();
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
