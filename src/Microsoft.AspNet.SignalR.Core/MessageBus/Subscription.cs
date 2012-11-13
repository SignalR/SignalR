// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
{
    public abstract class Subscription : ISubscription, IDisposable
    {
        private readonly Func<MessageResult, Task<bool>> _callback;
        private readonly IPerformanceCounterManager _counters;

        private int _state;
        private int _subscriptionState;

        private bool Alive
        {
            get
            {
                return _subscriptionState != SubscriptionState.Disposed;
            }
        }

        public string Identity { get; private set; }

        public HashSet<string> EventKeys { get; private set; }

        public int MaxMessages { get; private set; }

        public Action DisposedCallback { get; set; }

        protected Subscription(string identity, IEnumerable<string> eventKeys, Func<MessageResult, Task<bool>> callback, int maxMessages, IPerformanceCounterManager counters)
        {
            if (String.IsNullOrEmpty(identity))
            {
                throw new ArgumentNullException("identity");
            }

            if (eventKeys == null)
            {
                throw new ArgumentNullException("eventKeys");
            }

            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            if (maxMessages < 0)
            {
                throw new ArgumentOutOfRangeException("maxMessages");
            }

            if (counters == null)
            {
                throw new ArgumentNullException("counters");
            }

            Identity = identity;
            _callback = callback;
            EventKeys = new HashSet<string>(eventKeys);
            MaxMessages = maxMessages;
            _counters = counters;

            _counters.MessageBusSubscribersTotal.Increment();
            _counters.MessageBusSubscribersCurrent.Increment();
            _counters.MessageBusSubscribersPerSec.Increment();
        }

        public virtual Task<bool> Invoke(MessageResult result)
        {
            return Invoke(result, () => { });
        }

        private Task<bool> Invoke(MessageResult result, Action beforeInvoke)
        {
            // Change the state from idle to invoking callback
            var state = Interlocked.CompareExchange(ref _subscriptionState,
                                                    SubscriptionState.InvokingCallback,
                                                    SubscriptionState.Idle);

            if (state == SubscriptionState.Disposed)
            {
                // Only allow terminal messages after dispose
                if (!result.Terminal)
                {
                    return TaskAsyncHelper.False;
                }
            }

            beforeInvoke();

            return _callback.Invoke(result).ContinueWith(task =>
            {
                // Go from invoking callback to idle
                Interlocked.CompareExchange(ref _subscriptionState,
                                            SubscriptionState.Idle,
                                            SubscriptionState.InvokingCallback);

                if (task.IsFaulted)
                {
                    return TaskAsyncHelper.FromError<bool>(task.Exception);
                }

                return TaskAsyncHelper.FromResult(task.Result);
            },
            TaskContinuationOptions.ExecuteSynchronously).FastUnwrap();
        }

        public Task WorkAsync()
        {
            // Set the state to working
            Interlocked.Exchange(ref _state, State.Working);

            var tcs = new TaskCompletionSource<object>();

            WorkImpl(tcs);

            // Fast Path
            if (tcs.Task.IsCompleted)
            {
                return tcs.Task;
            }

            return FinishAsync(tcs);
        }

        public bool SetQueued()
        {
            return Interlocked.Increment(ref _state) == State.Working;
        }

        public bool UnsetQueued()
        {
            // If we try to set the state to idle and we were not already in the working state then keep going
            return Interlocked.CompareExchange(ref _state, State.Idle, State.Working) != State.Working;
        }

        private static Task FinishAsync(TaskCompletionSource<object> tcs)
        {
            return tcs.Task.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    return TaskAsyncHelper.FromError(task.Exception);
                }

                return TaskAsyncHelper.Empty;
            }).FastUnwrap();
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "We have a sync and async code path.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to avoid user code taking the process down.")]
        private void WorkImpl(TaskCompletionSource<object> taskCompletionSource)
        {
        Process:
            if (!Alive)
            {
                // If this subscription is dead then return immediately
                taskCompletionSource.TrySetResult(null);
                return;
            }

            var items = new List<ArraySegment<Message>>();
            int totalCount;
            object state;

            PerformWork(items, out totalCount, out state);

            if (items.Count > 0)
            {
                var messageResult = new MessageResult(items, totalCount);
                Task<bool> callbackTask = Invoke(messageResult, () => BeforeInvoke(state));

                if (callbackTask.IsCompleted)
                {
                    try
                    {
                        // Make sure exceptions propagate
                        callbackTask.Wait();

                        if (callbackTask.Result)
                        {
                            // Sync path
                            goto Process;
                        }
                        else
                        {
                            // If we're done pumping messages through to this subscription
                            // then dispose
                            Dispose();

                            // If the callback said it's done then stop
                            taskCompletionSource.TrySetResult(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Dispose if we failed to invoke the callback
                        Dispose();

                        taskCompletionSource.TrySetException(ex);
                    }
                }
                else
                {
                    WorkImplAsync(callbackTask, taskCompletionSource);
                }
            }
            else
            {
                taskCompletionSource.TrySetResult(null);
            }
        }

        protected virtual void BeforeInvoke(object state)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "The list needs to be populated")]
        [SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate", Justification = "The caller wouldn't be able to specify what the generic type argument is")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Justification = "The count needs to be returned")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "The state needs to be set by the callee")]
        protected abstract void PerformWork(List<ArraySegment<Message>> items, out int totalCount, out object state);

        private void WorkImplAsync(Task<bool> callbackTask, TaskCompletionSource<object> taskCompletionSource)
        {
            // Async path
            callbackTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // Dispose if we failed
                    Dispose();

                    taskCompletionSource.TrySetException(task.Exception);
                }
                else if (task.Result)
                {
                    WorkImpl(taskCompletionSource);
                }
                else
                {
                    // If we're done pumping messages through to this subscription
                    // then dispose
                    Dispose();

                    // If the callback said it's done then stop
                    taskCompletionSource.TrySetResult(null);
                }
            });
        }

        public virtual bool AddEvent(string key, Topic topic)
        {
            lock (EventKeys)
            {
                return EventKeys.Add(key);
            }
        }

        public virtual void RemoveEvent(string key)
        {
            lock (EventKeys)
            {
                EventKeys.Remove(key);
            }
        }

        public virtual void SetEventTopic(string key, Topic topic)
        {
            lock (EventKeys)
            {
                EventKeys.Add(key);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // REIVIEW: Consider sleeping instead of using a tight loop, or maybe timing out after some interval
                // if the client is very slow then this invoke call might not end quickly and this will make the CPU
                // hot waiting for the task to return.

                var spinWait = new SpinWait();

                while (true)
                {
                    // Wait until the subscription isn't working anymore
                    var state = Interlocked.CompareExchange(ref _subscriptionState,
                                                            SubscriptionState.Disposed,
                                                            SubscriptionState.Idle);

                    // If we're not working then stop
                    if (state != SubscriptionState.InvokingCallback)
                    {
                        if (state != SubscriptionState.Disposed)
                        {
                            // Only decrement if we're not disposed already
                            _counters.MessageBusSubscribersCurrent.Decrement();
                            _counters.MessageBusSubscribersPerSec.Decrement();
                        }

                        // Raise the disposed callback
                        if (DisposedCallback != null)
                        {
                            DisposedCallback();
                        }

                        break;
                    }

                    spinWait.SpinOnce();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "")]
        public abstract string GetCursor();

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Identity.Equals(((Subscription)obj).Identity);
        }

        private static class State
        {
            public const int Idle = 0;
            public const int Working = 1;
        }

        private static class SubscriptionState
        {
            public const int Idle = 0;
            public const int InvokingCallback = 1;
            public const int Disposed = 2;
        }
    }
}
