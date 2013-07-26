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
        private bool _disposed;

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
                DoWork(subscription);
            }
        }

        private void DoWork(ISubscription subscription)
        {
            _counters.MessageBusAllocatedWorkers.Increment();

            ThreadPool.UnsafeQueueUserWorkItem(async state =>
            {
                var sub = (ISubscription)state;
                do
                {
                    try
                    {
                        await sub.Work();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Failed to process work - " + ex.GetBaseException());
                        break;
                    }
                }
                while (sub.UnsetQueued());

                _counters.MessageBusAllocatedWorkers.Decrement();
            },
            subscription);
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
    }
}
