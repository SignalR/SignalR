using System;
using System.Threading;

namespace SignalR
{
    /// <summary>
    /// Helper class to manage disposing a resource at an arbirtary time
    /// </summary>
    internal class Disposer : IDisposable
    {
        private const int NeedDispose = 1;

        private int _state;
        private IDisposable _disposable;

        public void Set(IDisposable disposable)
        {
            _disposable = disposable;

            // Change the state to the need dispose state and dispose 
            if (Interlocked.Exchange(ref _state, NeedDispose) == NeedDispose)
            {
                disposable.Dispose();
            }
        }

        public void Dispose()
        {
            // If it's set, dispose it
            if (_disposable != null)
            {
                _disposable.Dispose();
            }
            else
            {
                // Change it to the should state
                Interlocked.Exchange(ref _state, NeedDispose);
            }
        }
    }
}
