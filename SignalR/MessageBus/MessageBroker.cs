using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// This class is the main coordinator. It schedules work to be done for a particular subscription 
    /// and has an algorithm for choosing a number of workers (thread pool threads), to handle
    /// the scheduled work.
    /// </summary>
    internal class MessageBroker : IDisposable
    {
        private readonly Queue<ISubscription> _queue = new Queue<ISubscription>();
        private readonly ConcurrentDictionary<string, Topic> _topics = new ConcurrentDictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);

        private readonly IPerformanceCounterManager _counters;

        // The maximum number of workers (threads) allowed to process all incoming messages
        private static readonly int MaxWorkers = 3 * Environment.ProcessorCount;

        // The maximum number of workers that can be left to idle (not busy but allocated)
        private static readonly int MaxIdleWorkers = Environment.ProcessorCount;

        // The number of allocated workers (currently running)
        private int _allocatedWorkers;

        // The number of workers that are *actually* doing work
        private int _busyWorkers;

        // The interval at which to check if there's work to be done
        private static readonly TimeSpan CheckWorkInterval = TimeSpan.FromSeconds(5);

        private Timer _timer;

        private int _checkingWork;

        public MessageBroker(ConcurrentDictionary<string, Topic> topics, IPerformanceCounterManager performanceCounterManager)
        {
            _topics = topics;
            _counters = performanceCounterManager;

            _timer = new Timer(_ => OnTimer(), state: null, dueTime: CheckWorkInterval, period: CheckWorkInterval);
        }

        public TraceSource Trace
        {
            get;
            set;
        }

        public int AllocatedWorkers
        {
            get
            {
                return _allocatedWorkers;
            }
        }

        public int BusyWorkers
        {
            get
            {
                return _busyWorkers;
            }
        }

        private void OnTimer()
        {
            if (Interlocked.Exchange(ref _checkingWork, 1) == 1)
            {
                return;
            }

            foreach (var topic in _topics.Values)
            {
                try
                {
                    topic.SubscriptionLock.EnterReadLock();

                    for (int i = 0; i < topic.Subscriptions.Count; i++)
                    {
                        ISubscription subscription = topic.Subscriptions[i];
                        Schedule(subscription);
                    }
                }
                finally
                {
                    topic.SubscriptionLock.ExitReadLock();
                }
            }

            Interlocked.Exchange(ref _checkingWork, 0);
        }

        public void Schedule(ISubscription subscription)
        {
            if (subscription.SetQueued())
            {
                lock (_queue)
                {
                    _queue.Enqueue(subscription);
                    Monitor.Pulse(_queue);
                    AddWorker();
                }
            }
        }

        public void AddWorker()
        {
            // Only create a new worker if everyone is busy (up to the max)
            if (_allocatedWorkers < MaxWorkers && _allocatedWorkers == _busyWorkers)
            {
                _counters.MessageBusAllocatedWorkers.RawValue = Interlocked.Increment(ref _allocatedWorkers);

                Trace.TraceInformation("Creating a worker, allocated={0}, busy={1}", _allocatedWorkers, _busyWorkers);

                ThreadPool.QueueUserWorkItem(ProcessWork);
            }
        }

        private void ProcessWork(object state)
        {
            Task pumpTask = PumpAsync();

            if (pumpTask.IsCompleted)
            {
                ProcessWorkSync(pumpTask);
            }
            else
            {
                ProcessWorkAsync(pumpTask);
            }

        }

        private void ProcessWorkSync(Task pumpTask)
        {
            try
            {
                pumpTask.Wait();
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Failed to process work - " + ex.GetBaseException());
            }
            finally
            {
                // After the pump runs decrement the number of workers in flight
                _counters.MessageBusAllocatedWorkers.RawValue = Interlocked.Decrement(ref _allocatedWorkers);
            }
        }

        private void ProcessWorkAsync(Task pumpTask)
        {
            pumpTask.ContinueWith(task =>
            {
                // After the pump runs decrement the number of workers in flight
                _counters.MessageBusAllocatedWorkers.RawValue = Interlocked.Decrement(ref _allocatedWorkers);

                if (task.IsFaulted)
                {
                    Trace.TraceInformation("Failed to process work - " + task.Exception.GetBaseException());
                }
            });
        }

        public Task PumpAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            PumpImpl(tcs);
            return tcs.Task;
        }

        public void PumpImpl(TaskCompletionSource<object> taskCompletionSource)
        {

        Process:

            Debug.Assert(_allocatedWorkers <= MaxWorkers, "How did we pass the max?");

            // If we're withing the acceptable limit of idleness, just keep running
            int idleWorkers = _allocatedWorkers - _busyWorkers;
            if (idleWorkers <= MaxIdleWorkers)
            {
                ISubscription subscription;

                lock (_queue)
                {
                    while (_queue.Count == 0)
                    {
                        Monitor.Wait(_queue);
                    }

                    subscription = _queue.Dequeue();
                }

                _counters.MessageBusBusyWorkers.RawValue = Interlocked.Increment(ref _busyWorkers);
                Task workTask = subscription.WorkAsync();

                if (workTask.IsCompleted)
                {
                    try
                    {
                        workTask.Wait();

                        goto Process;
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                    finally
                    {
                        subscription.UnsetQueued();
                        _counters.MessageBusBusyWorkers.RawValue = Interlocked.Decrement(ref _busyWorkers);

                        Debug.Assert(_busyWorkers >= 0, "The number of busy workers has somehow gone negative");
                    }
                }
                else
                {
                    PumpImplAsync(workTask, subscription, taskCompletionSource);
                }
            }
            else
            {
                taskCompletionSource.TrySetResult(null);
            }
        }

        private void PumpImplAsync(Task workTask, ISubscription subscription, TaskCompletionSource<object> taskCompletionSource)
        {
            // Async path
            workTask.ContinueWith(task =>
            {
                subscription.UnsetQueued();
                _counters.MessageBusBusyWorkers.RawValue = Interlocked.Decrement(ref _busyWorkers);

                Debug.Assert(_busyWorkers >= 0, "The number of busy workers has somehow gone negative");

                if (task.IsFaulted)
                {
                    taskCompletionSource.TrySetException(task.Exception);
                }
                else
                {
                    PumpImpl(taskCompletionSource);
                }
            });
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
