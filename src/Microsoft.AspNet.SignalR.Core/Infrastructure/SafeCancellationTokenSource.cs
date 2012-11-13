using System;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// Thread safe cancellation token source. Allows the following:
    /// - Cancel can only be called once and will no-op if the token is disposed.
    /// - Dispose can only be called once.
    /// - Dispose may be called after Cancel.
    /// </summary>
    internal class SafeCancellationTokenSource : IDisposable
    {
        private CancellationTokenSource _cts;
        private int _state;

        public SafeCancellationTokenSource()
        {
            _cts = new CancellationTokenSource();
            Token = _cts.Token;
        }

        public CancellationToken Token { get; private set; }

        public void Cancel()
        {
            var value = Interlocked.CompareExchange(ref _state, State.Cancelled, State.Initial);

            if (value == State.Initial)
            {
                _cts.Cancel();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var value = Interlocked.Exchange(ref _state, State.Disposed);

                // Only dispose if not already disposed
                if (value != State.Disposed)
                {
                    _cts.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private static class State
        {
            public const int Initial = 0;
            public const int Cancelled = 1;
            public const int Disposed = 2;
        }
    }
}
