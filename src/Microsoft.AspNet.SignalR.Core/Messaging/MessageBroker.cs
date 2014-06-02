// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    /// <summary>
    /// This class is the main coordinator. 
    /// It schedules work to be done for a particular subscription. 
    /// </summary>
    public class MessageBroker : IDisposable
    {
        private readonly IPerformanceCounterManager _counters;

        // Determines if the broker was disposed and should stop doing all work.
        private volatile bool _disposed;

        public MessageBroker(IPerformanceCounterManager performanceCounterManager)
        {
            _counters = performanceCounterManager;
        }

        public TraceSource Trace
        {
            get;
            set;
        }

        public void Schedule(ISubscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException("subscription");
            }

            if (_disposed)
            {
                // Don't queue up new work if we've disposed the broker
                return;
            }

            if (subscription.SetQueued())
            {
                // This is a worker
                ScheduleWork(subscription);
            }
        }

        private void ScheduleWork(ISubscription subscription)
        {
            var workContext = new WorkContext(subscription, this);

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                var context = (WorkContext)state;

                context.Broker._counters.MessageBusAllocatedWorkers.Increment();

                DoWork(context);

                context.Broker._counters.MessageBusAllocatedWorkers.Decrement();
            }, 
            workContext);
        }

        private static async void DoWork(WorkContext context)
        {
            do
            {
                try
                {
                    context.Broker._counters.MessageBusBusyWorkers.Increment();

                    await context.Subscription.Work();
                }
                catch (Exception ex)
                {
                    context.Broker.Trace.TraceError("Failed to process work - " + ex.GetBaseException());
                    break;
                }
                finally
                {
                    context.Broker._counters.MessageBusBusyWorkers.Decrement();
                }
            }
            while (context.Subscription.UnsetQueued() && !context.Broker._disposed);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private class WorkContext
        {
            public ISubscription Subscription;
            public MessageBroker Broker;

            public WorkContext(ISubscription subscription, MessageBroker broker)
            {
                Subscription = subscription;
                Broker = broker;
            }
        }
    }
}
