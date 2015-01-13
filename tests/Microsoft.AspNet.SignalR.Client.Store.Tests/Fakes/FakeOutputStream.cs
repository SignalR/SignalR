
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeOutputStream : IOutputStream, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();

        public IAsyncOperation<bool> FlushAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            _invocationManager.AddInvocation("WriteAsync", buffer);

            return new FakeAsyncOperationWithProgress();
        }

        public void Dispose()
        {
        }

        private class FakeAsyncOperationWithProgress : IAsyncOperationWithProgress<uint, uint>
        {
            private AsyncOperationWithProgressCompletedHandler<uint, uint> _completed;

            public AsyncOperationWithProgressCompletedHandler<uint, uint> Completed
            {
                get { return _completed; }
                set
                {
                    _completed = value;
                    _completed(this, AsyncStatus.Completed);
                }
            }

            public uint GetResults()
            {
                return 0;
            }

            public AsyncOperationProgressHandler<uint, uint> Progress
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public void Cancel()
            {
                throw new NotImplementedException();
            }

            public void Close()
            {
                throw new NotImplementedException();
            }

            public Exception ErrorCode
            {
                get { throw new NotImplementedException(); }
            }

            public uint Id
            {
                get { throw new NotImplementedException(); }
            }

            public AsyncStatus Status
            {
                get { throw new NotImplementedException(); }
            }
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
