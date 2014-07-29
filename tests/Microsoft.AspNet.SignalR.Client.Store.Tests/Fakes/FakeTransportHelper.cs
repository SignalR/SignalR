using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeTransportHelper : TransportHelper, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();

        public override bool ProcessResponse(IConnection connection, string response, Action onInitialized)
        {
            _invocationManager.AddInvocation("ProcessResponse", connection, response, onInitialized);

            return _invocationManager.GetReturnValue<bool>("ProcessResponse");
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
