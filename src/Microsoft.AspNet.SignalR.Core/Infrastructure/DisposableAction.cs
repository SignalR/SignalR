// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class DisposableAction : IDisposable
    {
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification="The client projects use this.")]
        public static readonly DisposableAction Empty = new DisposableAction(() => { });

        private Action _action;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Exchange(ref _action, () => { }).Invoke();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

}
