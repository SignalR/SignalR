﻿using System;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal static class CancellationTokenExtensions
    {
        public static IDisposable SafeRegister<T>(this CancellationToken cancellationToken, Action<T> callback, T state)
        {
            var callbackInvoked = 0;

            try
            {
                CancellationTokenRegistration registration = cancellationToken.Register(callbackState =>
                {
                    if (Interlocked.Exchange(ref callbackInvoked, 1) == 0)
                    {
                        callback((T)callbackState);
                    }
                },
                state,
                useSynchronizationContext: false);

                return new DisposableAction(() =>
                {
                    // This normally waits until the callback is finished invoked but we don't care
                    if (Interlocked.Exchange(ref callbackInvoked, 1) == 0)
                    {
                        registration.Dispose();
                    }
                });
            }
            catch (ObjectDisposedException)
            {
                if (Interlocked.Exchange(ref callbackInvoked, 1) == 0)
                {
                    callback(state);
                }
            }

            // Noop
            return new DisposableAction(() => { });
        }
    }
}
