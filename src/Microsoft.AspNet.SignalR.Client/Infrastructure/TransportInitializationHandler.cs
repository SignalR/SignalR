// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class TransportInitializationHandler
    {
        private ThreadSafeInvoker _initializationInvoker;
        private TaskCompletionSource<object> _initializationTask;
        private IDisposable _tokenCleanup;

        public TransportInitializationHandler(TimeSpan failureTimeout, CancellationToken disconnectToken)
        {
            _initializationTask = new TaskCompletionSource<object>();
            _initializationInvoker = new ThreadSafeInvoker();

            // Default event
            OnFailure = () => { };

            // We want to fail if the disconnect token is tripped while we're waiting on initialization
            _tokenCleanup = disconnectToken.SafeRegister(_ =>
            {
                Failure(null);
            }, null);

            TaskAsyncHelper.Delay(failureTimeout).Then(() =>
            {
                Failure(new TimeoutException(Resources.Error_TransportTimedOutTryingToConnect));
            });
        }

        public event Action OnFailure;

        public Task Task
        {
            get
            {
                return _initializationTask.Task;
            }
        }

        public void Success()
        {
            _initializationInvoker.Invoke(() =>
            {
                _initializationTask.SetResult(null);
                _tokenCleanup.Dispose();
            });
        }

        public void Failure(Exception ex)
        {
            _initializationInvoker.Invoke(() =>
            {
                OnFailure();
                _initializationTask.SetException(ex);
                _tokenCleanup.Dispose();
            });
        }
    }
}
