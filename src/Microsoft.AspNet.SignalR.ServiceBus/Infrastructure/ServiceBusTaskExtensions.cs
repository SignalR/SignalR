// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus.Infrastructure
{
    public static class ServiceBusTaskExtensions
    {
        // Stephen Toub http://blogs.msdn.com/b/pfxteam/archive/2011/06/27/10179452.aspx
        static Task<TResult> ToApm<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<TResult>(state);

            task.ContinueWith(delegate
            {
                if (task.IsFaulted) tcs.TrySetException(task.Exception.InnerExceptions);
                else if (task.IsCanceled) tcs.TrySetCanceled();
                else tcs.TrySetResult(task.Result);

                callback?.Invoke(tcs.Task);

            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

            return tcs.Task;
        }

        public static IAsyncResult BeginReceiveBatch(this MessageReceiver mr, int messageCount,
            TimeSpan timeSpan, AsyncCallback callback, object state)
        {
            return mr.ReceiveBatchAsync(messageCount, timeSpan).ToApm(callback, state);
        }

        public static IEnumerable<BrokeredMessage> EndReceiveBatch(this MessageReceiver mr, IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<IEnumerable<BrokeredMessage>>)asyncResult).Result;
            }
            catch (AggregateException ae) { throw ae.InnerException; }
        }

    }
}
