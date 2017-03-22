// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class DispatchingTaskCompletionSource<TResult>
    {
        private readonly TaskCompletionSource<TResult> _tcs = new TaskCompletionSource<TResult>();

        public Task<TResult> Task
        {
            get { return _tcs.Task; }
        }

        public void SetCanceled()
        {
            TaskAsyncHelper.Dispatch(() => _tcs.SetCanceled());
        }

        public void SetException(Exception exception)
        {
            TaskAsyncHelper.Dispatch(() => _tcs.SetException(exception));
        }

        public void SetException(IEnumerable<Exception> exceptions)
        {
            TaskAsyncHelper.Dispatch(() => _tcs.SetException(exceptions));
        }

        public void SetResult(TResult result)
        {
            TaskAsyncHelper.Dispatch(() => _tcs.SetResult(result));
        }

        public void TrySetCanceled()
        {
            TaskAsyncHelper.Dispatch(() => _tcs.TrySetCanceled());
        }

        public void TrySetException(Exception exception)
        {
            TaskAsyncHelper.Dispatch(() => _tcs.TrySetException(exception));
        }

        public void TrySetException(IEnumerable<Exception> exceptions)
        {
            TaskAsyncHelper.Dispatch(() => _tcs.TrySetException(exceptions));
        }

        public void TrySetUnwrappedException(Exception e)
        {
            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                _tcs.TrySetException(aggregateException.InnerExceptions);
            }
            else
            {
                _tcs.TrySetException(e);
            }
        }

        public void TrySetResult(TResult result)
        {
            TaskAsyncHelper.Dispatch(() => _tcs.TrySetResult(result));
        }
    }
}
