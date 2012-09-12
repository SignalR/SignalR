using System;
using System.Threading;

namespace SignalR
{
    internal class Disposer : IDisposable
    {
        private int _state;
        private IDisposable _disposable;

        public void Set(IDisposable disposable)
        {
            _disposable = disposable;

            if (Interlocked.Exchange(ref _state, 1) == 1)
            {
                disposable.Dispose();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _state, 1) == 1)
            {
                _disposable.Dispose();
            }
        }
    }
}
