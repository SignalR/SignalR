
using System;
using System.Collections.Generic;
using Windows.Storage.Streams;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeWebSocketResponse : IWebSocketResponse, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();

        public IDataReader GetDataReader()
        {
            return _invocationManager.GetReturnValue<IDataReader>("GetDataReader");
        }

        public void Setup<T>(string methodName, Func<T> behavior)
        {
            _invocationManager.AddSetup(methodName, behavior);
        }

        void IFake.Verify(string methodName, System.Collections.Generic.List<object[]> expectedParameters)
        {
            throw new NotImplementedException();
        }

        IEnumerable<object[]> IFake.GetInvocations(string methodName)
        {
            throw new NotImplementedException();
        }
    }
}
