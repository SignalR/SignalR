using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class AsyncManualResetEvent : IDisposable
    {
        private ManualResetEventSlim _mre;

        public AsyncManualResetEvent()
        {
            _mre = new ManualResetEventSlim();
        }

        public AsyncManualResetEvent(bool initialState)
        {
            _mre = new ManualResetEventSlim(initialState);
        }

        public async Task<bool> WaitAsync(TimeSpan timeout)
        {
            return await Task.Factory.StartNew<bool>(() =>
            {
                return _mre.Wait(timeout);
            });
        }

        public void Set() 
        {
            _mre.Set();
        }

        public void Reset()
        {
            _mre.Reset();
        }

        public void Dispose()
        {
            _mre.Dispose();
        }
    }
}
