// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// Helper class to manage disposing a resource at an arbirtary time
    /// </summary>
    internal class Disposer : IDisposable
    {
        private static readonly object _disposedSentinel = new object();

        private object _disposable;

        public void Set(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException("disposable");
            }

            object originalFieldValue = Interlocked.CompareExchange(ref _disposable, disposable, null);
            if (originalFieldValue == null)
            {
                // this is the first call to Set() and Dispose() hasn't yet been called; do nothing
            }
            else if (originalFieldValue == _disposedSentinel)
            {
                // Dispose() has already been called, so we need to dispose of the object that was just added
                disposable.Dispose();
            }
            else
            {
                // Set has been called multiple times, fail
                Debug.Fail("Multiple calls to Disposer.Set(IDisposable) without calling Disposer.Dispose()");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var disposable = Interlocked.Exchange(ref _disposable, _disposedSentinel) as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

#elif NET40
// Not used in this framework.
#else 
#error Unsupported target framework.
#endif
