// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal class DisposableAction : IDisposable
    {
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
