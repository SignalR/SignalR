// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    internal class NegotiateInitializer
    {
        private readonly ThreadSafeInvoker _callbackInvoker;
        private readonly Action _initializeCallback;
        private readonly Action<Exception> _errorCallback;

        public NegotiateInitializer(TransportInitializationHandler initializeHandler)
        {
            _initializeCallback = initializeHandler.Success;
            _errorCallback = initializeHandler.Fail;
            _callbackInvoker = new ThreadSafeInvoker();

            // Set default initialized function
            Initialized += () => { };
        }

        public event Action Initialized;

        public void Complete()
        {
            _callbackInvoker.Invoke(() =>
            {
                Initialized();
                _initializeCallback();
            });
        }

        public void Complete(Exception exception)
        {
            _callbackInvoker.Invoke((cb, ex) =>
            {
                Initialized();
                cb(ex);
            }, _errorCallback, exception);
        }

        public void Abort(CancellationToken disconnectToken)
        {
            _callbackInvoker.Invoke((cb, token) =>
            {
                Initialized();
                cb(new OperationCanceledException(Resources.Error_ConnectionCancelled, token));
            }, _errorCallback, disconnectToken);
        }
    }
}
