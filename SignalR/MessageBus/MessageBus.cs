using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public class MessageBus : INewMessageBus
    {
        private readonly ConcurrentDictionary<string, Topic> _topics = new ConcurrentDictionary<string, Topic>();
        private readonly Engine _engine;

        private const int DefaultMessageStoreSize = 1000;

        private readonly ITraceManager _trace;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        public MessageBus(IDependencyResolver resolver)
            : this(resolver.Resolve<ITraceManager>())
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traceManager"></param>
        public MessageBus(ITraceManager traceManager)
        {
            _trace = traceManager;
            _engine = new Engine(_topics)
            {
                Trace = Trace
            };
        }

        private TraceSource Trace
        {
            get
            {
                return _trace["SignalR.MessageBus"];
            }
        }

        public int AllocatedWorkers
        {
            get
            {
                return _engine.AllocatedWorkers;
            }
        }

        public int BusyWorkers
        {
            get
            {
                return _engine.BusyWorkers;
            }
        }

        /// <summary>
        /// Publishes a new message to the specified event on the bus.
        /// </summary>
        /// <param name="source">A value representing the source of the data sent.</param>
        /// <param name="eventKey">The specific event key to send data to.</param>
        /// <param name="value">The value to send.</param>
        public Task Publish(string source, string eventKey, object value)
        {
            Topic topic = _topics.GetOrAdd(eventKey, _ => new Topic());

            topic.Store.Add(new Message(eventKey, value));

            foreach (var subscription in topic.Subscriptions.GetSnapshot())
            {
                _engine.Schedule(subscription);
            }

            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="cursor"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<Exception, MessageResult, Task> callback)
        {
            IEnumerable<Cursor> cursors = null;
            if (cursor == null)
            {
                cursors = from key in subscriber.EventKeys
                          select new Cursor
                          {
                              Key = key,
                              Id = GetMessageId(key)
                          };
            }
            else
            {
                cursors = Cursor.GetCursors(cursor);
            }

            var subscription = new Subscription(cursors, callback);

            foreach (var key in subscriber.EventKeys)
            {
                var topic = _topics.GetOrAdd(key, _ => new Topic());
                topic.Subscriptions.Add(subscription);
            }

            if (!String.IsNullOrEmpty(cursor))
            {
                // Update all of the cursors so we're within the range
                foreach (var pair in subscription.Cursors)
                {
                    Topic topic;
                    if (_topics.TryGetValue(pair.Key, out topic) && pair.Id > topic.Store.GetMessageCount())
                    {
                        subscription.UpdateCursor(pair.Key, 0);
                    }
                }
            }

            Action<string, string> eventAdded = (eventKey, eventCursor) =>
            {
                Topic topic = _topics.GetOrAdd(eventKey, _ => new Topic());
                ulong id = eventCursor == null ? 0 : UInt64.Parse(eventCursor);
                subscription.AddOrUpdateCursor(eventKey, id);
                topic.Subscriptions.Add(subscription);
            };

            Action<string> eventRemoved = eventKey => RemoveEvent(subscription, eventKey);

            subscriber.EventAdded += eventAdded;
            subscriber.EventRemoved += eventRemoved;

            return new DisposableAction(() =>
            {
                subscriber.EventAdded -= eventAdded;
                subscriber.EventRemoved -= eventRemoved;

                string currentCursor = Cursor.MakeCursor(subscription.Cursors);
                subscription.Invoke(new MessageResult(currentCursor));

                foreach (var eventKey in subscriber.EventKeys)
                {
                    RemoveEvent(subscription, eventKey);
                }
            });
        }

        private void RemoveEvent(Subscription subscription, string eventKey)
        {
            Topic topic;
            if (_topics.TryGetValue(eventKey, out topic))
            {
                topic.Subscriptions.Remove(subscription);
                subscription.RemoveCursor(eventKey);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventKey"></param>
        /// <returns></returns>
        public string GetCursor(string eventKey)
        {
            return GetMessageId(eventKey).ToString();
        }

        private ulong GetMessageId(string key)
        {
            Topic topic;
            if (_topics.TryGetValue(key, out topic))
            {
                return topic.Store.GetMessageCount();
            }

            return 0;
        }

        private class Subscription
        {
            private readonly List<Cursor> _cursors;
            private readonly Func<Exception, MessageResult, Task> _callback;

            private readonly ReaderWriterLockSlim _cursorLock = new ReaderWriterLockSlim();

            private int _queued;
            private int _working;

            public Subscription(IEnumerable<Cursor> cursors, Func<Exception, MessageResult, Task> callback)
            {
                _cursors = new List<Cursor>(cursors);
                _callback = callback;
            }

            public int CursorCount
            {
                get
                {
                    try
                    {
                        _cursorLock.EnterReadLock();
                        return _cursors.Count;
                    }
                    finally
                    {
                        _cursorLock.ExitReadLock();
                    }
                }
            }

            public IEnumerable<Cursor> Cursors
            {
                get
                {
                    try
                    {
                        _cursorLock.EnterReadLock();
                        return _cursors;
                    }
                    finally
                    {
                        _cursorLock.ExitReadLock();
                    }
                }
            }

            public Task Invoke(MessageResult result)
            {
                return _callback.Invoke(null, result);
            }

            public Task WorkAsync(ConcurrentDictionary<string, Topic> topics)
            {
                if (SetWorking())
                {
                    var tcs = new TaskCompletionSource<object>();

                    WorkImpl(topics, tcs);

                    // Fast Path
                    if (tcs.Task.IsCompleted)
                    {
                        UnsetWorking();
                        return tcs.Task;
                    }

                    return FinishAsync(tcs);
                }

                return TaskAsyncHelper.Empty;
            }

            public bool SetQueued()
            {
                return Interlocked.Exchange(ref _queued, 1) == 0;
            }

            public bool UnsetQueued()
            {
                return Interlocked.Exchange(ref _queued, 0) == 1;
            }

            private bool SetWorking()
            {
                return Interlocked.Exchange(ref _working, 1) == 0;
            }

            private bool UnsetWorking()
            {
                return Interlocked.Exchange(ref _working, 0) == 1;
            }

            private Task FinishAsync(TaskCompletionSource<object> tcs)
            {
                return tcs.Task.ContinueWith(task =>
                {
                    UnsetWorking();

                    if (task.IsFaulted)
                    {
                        return TaskAsyncHelper.FromError(task.Exception);
                    }

                    return TaskAsyncHelper.Empty;
                }).FastUnwrap();
            }

            private void WorkImpl(ConcurrentDictionary<string, Topic> topics, TaskCompletionSource<object> taskCompletionSource)
            {

            Process:
                // Reserve 25 messages per cursor
                int cursorCount = CursorCount;
                var messages = new Message[cursorCount * 25];
                var cursors = new List<Cursor>(cursorCount);

                int count = 0;
                foreach (var cursor in Cursors)
                {
                    Topic topic;
                    if (topics.TryGetValue(cursor.Key, out topic))
                    {
                        MessageStoreResult<Message> storeResult = topic.Store.GetMessages(cursor.Id);
                        ulong next = storeResult.FirstMessageId + (ulong)storeResult.Messages.Length;
                        cursor.Id = next;

                        if (storeResult.Messages.Length > 0)
                        {
                            // We ran out of space
                            int need = count + storeResult.Messages.Length;
                            if (need >= messages.Length)
                            {
                                // Double the length
                                Array.Resize(ref messages, messages.Length * 2);
                            }

                            // Copy the paylod
                            Array.Copy(storeResult.Messages, 0, messages, count, storeResult.Messages.Length);
                            count += storeResult.Messages.Length;
                        }

                        if (cursor.Id > 0)
                        {
                            // Add this cursor to the list
                            cursors.Add(cursor);
                        }
                    }
                }

                if (count > 0)
                {
                    // REVIEW: Should we change this to not resize and have the callers detect null?
                    if (count < messages.Length)
                    {
                        Array.Resize(ref messages, count);
                    }

                    var messageResult = new MessageResult(messages, Cursor.MakeCursor(cursors));
                    Task callbackTask = Invoke(messageResult);

                    if (callbackTask.IsCompleted)
                    {
                        try
                        {
                            // Make sure exceptions propagate
                            callbackTask.Wait();

                            // Sync path
                            goto Process;
                        }
                        catch (Exception ex)
                        {
                            taskCompletionSource.TrySetException(ex);
                        }
                    }
                    else
                    {
                        WorkImplAsync(callbackTask, topics, taskCompletionSource);
                    }
                }
                else
                {
                    taskCompletionSource.TrySetResult(null);
                }
            }

            private void WorkImplAsync(Task callbackTask, ConcurrentDictionary<string, Topic> topics, TaskCompletionSource<object> taskCompletionSource)
            {
                // Async path
                callbackTask.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        taskCompletionSource.TrySetException(task.Exception);
                    }
                    else
                    {
                        WorkImpl(topics, taskCompletionSource);
                    }
                });
            }

            public void AddOrUpdateCursor(string key, ulong id)
            {
                try
                {
                    _cursorLock.EnterWriteLock();

                    // O(n), but small n and it's not common
                    var index = _cursors.FindIndex(c => c.Key == key);
                    if (index != -1)
                    {
                        _cursors[index].Id = id;
                    }
                    else
                    {
                        _cursors.Add(new Cursor
                        {
                            Key = key,
                            Id = id
                        });
                    }
                }
                finally
                {
                    _cursorLock.ExitWriteLock();
                }
            }

            public bool UpdateCursor(string key, ulong id)
            {
                try
                {
                    _cursorLock.EnterWriteLock();

                    // O(n), but small n and it's not common
                    var index = _cursors.FindIndex(c => c.Key == key);
                    if (index != -1)
                    {
                        _cursors[index].Id = id;
                        return true;
                    }

                    return false;
                }
                finally
                {
                    _cursorLock.ExitWriteLock();
                }
            }

            public void RemoveCursor(string eventKey)
            {
                try
                {
                    _cursorLock.EnterWriteLock();
                    _cursors.RemoveAll(c => c.Key == eventKey);
                }
                finally
                {
                    _cursorLock.ExitWriteLock();
                }
            }
        }

        internal class Cursor
        {
            public string Key { get; set; }

            public ulong Id { get; set; }

            public static string MakeCursor(IEnumerable<Cursor> cursors)
            {
                var sb = new StringBuilder();
                bool first = true;
                foreach (var c in cursors)
                {
                    if (!first)
                    {
                        sb.Append('|');
                    }
                    sb.Append(Escape(c.Key));
                    sb.Append(',');
                    sb.Append(c.Id);
                    first = false;
                }

                return sb.ToString();
            }

            private static string Escape(string value)
            {
                var sb = new StringBuilder();
                // \\ = \
                // \| = |
                // \, = ,
                foreach (var ch in value)
                {
                    switch (ch)
                    {
                        case '\\':
                            sb.Append('\\').Append(ch);
                            break;
                        case '|':
                            sb.Append('\\').Append(ch);
                            break;
                        case ',':
                            sb.Append('\\').Append(ch);
                            break;
                        default:
                            sb.Append(ch);
                            break;
                    }
                }

                return sb.ToString();
            }

            public static Cursor[] GetCursors(string cursor)
            {
                var cursors = new List<Cursor>();
                var current = new Cursor();
                bool escape = false;
                var sb = new StringBuilder();

                foreach (var ch in cursor)
                {
                    if (escape)
                    {
                        sb.Append(ch);
                        escape = false;
                    }
                    else
                    {
                        if (ch == '\\')
                        {
                            escape = true;
                        }
                        else if (ch == ',')
                        {
                            current.Key = sb.ToString();
                            sb.Clear();
                        }
                        else if (ch == '|')
                        {
                            current.Id = UInt64.Parse(sb.ToString());
                            cursors.Add(current);
                            current = new Cursor();
                            sb.Clear();
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                    }
                }

                if (sb.Length > 0)
                {
                    current.Id = UInt64.Parse(sb.ToString());
                    cursors.Add(current);
                }

                return cursors.ToArray();
            }
        }

        private class Topic
        {
            public SafeSet<Subscription> Subscriptions { get; set; }
            public MessageStore<Message> Store { get; set; }

            public Topic()
            {
                Subscriptions = new SafeSet<Subscription>();
                Store = new MessageStore<Message>(DefaultMessageStoreSize);
            }
        }

        /// <summary>
        /// This class is the main coordinator. It schedules work to be done for a particular subscription 
        /// and has an algorithm for choosing a number of workers (thread pool threads), to handle
        /// the scheduled work.
        /// </summary>
        private class Engine
        {
            private readonly BlockingCollection<Subscription> _queue = new BlockingCollection<Subscription>();
            private readonly ConcurrentDictionary<string, Topic> _topics = new ConcurrentDictionary<string, Topic>();

            // The maximum number of workers (threads) allowed to process all incoming messages
            private const int MaxWorkers = 10;

            // The maximum number of workers that can be left to idle (not busy but allocated)
            private const int MaxIdleWorkers = 5;

            // The number of allocated workers (currently running)
            private int _allocatedWorkers;

            // The number of workers that are *actually* doing work
            private int _busyWorkers;

            public Engine(ConcurrentDictionary<string, Topic> topics)
            {
                _topics = topics;
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

            public void Schedule(Subscription subscription)
            {
                if (subscription.SetQueued() && _queue.TryAdd(subscription))
                {
                    AddWorker();
                }
            }

            public void AddWorker()
            {
                // Only create a new worker if everyone is busy (up to the max)
                if (_allocatedWorkers < MaxWorkers && _allocatedWorkers == _busyWorkers)
                {
                    Interlocked.Increment(ref _allocatedWorkers);

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
                    Interlocked.Decrement(ref _allocatedWorkers);
                }
            }

            private void ProcessWorkAsync(Task pumpTask)
            {
                pumpTask.ContinueWith(task =>
                {
                    // After the pump runs decrement the number of workers in flight
                    Interlocked.Decrement(ref _allocatedWorkers);

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
                    Subscription subscription = _queue.Take();

                    Interlocked.Increment(ref _busyWorkers);
                    Task workTask = subscription.WorkAsync(_topics);

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
                            Interlocked.Decrement(ref _busyWorkers);

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

            private void PumpImplAsync(Task workTask, Subscription subscription, TaskCompletionSource<object> taskCompletionSource)
            {
                // Async path
                workTask.ContinueWith(task =>
                {
                    subscription.UnsetQueued();
                    Interlocked.Decrement(ref _busyWorkers);

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
        }
    }
}
