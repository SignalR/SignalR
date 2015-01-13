using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal interface IFake
    {
        void Setup<T>(string methodName, Func<T> behavior);
        void Verify(string methodName, List<object[]> expectedParameters);
        IEnumerable<object[]> GetInvocations(string methodName);
    }
}
