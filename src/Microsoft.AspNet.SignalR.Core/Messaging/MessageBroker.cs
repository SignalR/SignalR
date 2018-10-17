// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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

            ThreadPool.UnsafeQueueUserWorkItem(Worker, workContext);

            async void Worker(object state)
            {
                var context = (WorkContext)state;

                context.Broker._counters.MessageBusAllocatedWorkers.Increment();

                await DoWork(context);

                context.Broker._counters.MessageBusAllocatedWorkers.Decrement();
            }
        }

        internal static async Task DoWork(WorkContext context)
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

                    // Dispose the subscription, or we might leak memory from it.
                    (context.Subscription as IDisposable)?.Dispose();
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

        internal class WorkContext
        {
            public ISubscription Subscription { get; }
            public MessageBroker Broker { get; }

            public WorkContext(ISubscription subscription, MessageBroker broker)
            {
                Subscription = subscription;
                Broker = broker;
            }
        }
    }
}
