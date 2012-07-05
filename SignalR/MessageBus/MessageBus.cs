﻿using System;
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

        private const int DefaultMessageStoreSize = 5000;

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

            foreach (var eventKey in subscriber.EventKeys)
            {
                var topic = _topics.GetOrAdd(eventKey, _ => new Topic());

                // Set the subscription for this topic
                subscription.SetCursorTopic(eventKey, topic);

                // Add this subscription to the list of subs
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

                // Get the cursor for this event key
                ulong id = eventCursor == null ? 0 : UInt64.Parse(eventCursor);

                // Add or update the cursor (in case it already exists)
                subscription.AddOrUpdateCursor(eventKey, id);

                // Set the topic
                subscription.SetCursorTopic(eventKey, topic);

                // Add it to the list of subs
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

        internal class Subscription
        {
            private readonly List<Cursor> _cursors;
            private readonly Func<Exception, MessageResult, Task> _callback;

            private readonly object _lockObj = new object();

            private int _queued;
            private int _working;

            // Pre-allocated buffer to use when working starts
            private Message[] _buffer;

            public IList<Cursor> Cursors
            {
                get
                {
                    return _cursors;
                }
            }

            public Subscription(IEnumerable<Cursor> cursors, Func<Exception, MessageResult, Task> callback)
            {
                _cursors = new List<Cursor>(cursors);
                _callback = callback;
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

                    // Allocate the buffer when the work starts
                    _buffer = new Message[25];

                    WorkImpl(topics, tcs);

                    // Fast Path
                    if (tcs.Task.IsCompleted)
                    {
                        // Kill the buffer
                        _buffer = null;

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
                    _buffer = null;

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
                int count;
                string nextCursor = null;

                lock (_lockObj)
                {
                    count = 0;
                    for (int i = 0; i < Cursors.Count; i++)
                    {
                        Cursor cursor = Cursors[i];
                        MessageStoreResult<Message> storeResult = cursor.Topic.Store.GetMessages(cursor.Id);
                        ulong next = storeResult.FirstMessageId + (ulong)storeResult.Messages.Count;
                        cursor.Id = next;

                        if (storeResult.Messages.Count > 0)
                        {
                            // We ran out of space
                            int need = count + storeResult.Messages.Count;
                            while (need >= _buffer.Length)
                            {
                                // Double the length
                                Array.Resize(ref _buffer, _buffer.Length * 2);
                            }

                            // Copy the paylod
                            Array.Copy(storeResult.Messages.Array, storeResult.Messages.Offset, _buffer, count, storeResult.Messages.Count);
                            count += storeResult.Messages.Count;
                        }
                    }

                    nextCursor = Cursor.MakeUpdatedCursor(Cursors);
                }

                if (count > 0)
                {
                    var messageResult = new MessageResult(_buffer, nextCursor, count);
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
                lock (_lockObj)
                {
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
            }

            public bool UpdateCursor(string key, ulong id)
            {
                lock (_lockObj)
                {
                    // O(n), but small n and it's not common
                    var index = _cursors.FindIndex(c => c.Key == key);
                    if (index != -1)
                    {
                        _cursors[index].Id = id;
                        return true;
                    }

                    return false;
                }
            }

            public void RemoveCursor(string eventKey)
            {
                lock (_lockObj)
                {
                    _cursors.RemoveAll(c => c.Key == eventKey);
                }
            }

            public void SetCursorTopic(string key, Topic topic)
            {
                lock (_lockObj)
                {
                    // O(n), but small n and it's not common
                    var index = _cursors.FindIndex(c => c.Key == key);
                    if (index != -1)
                    {
                        _cursors[index].Topic = topic;
                    }
                }
            }
        }

        internal class Cursor
        {
            private static char[] _escapeChars = new[] { '\\', '|', ',' };

            private string _key;
            public string Key
            {
                get
                {
                    return _key;
                }
                set
                {
                    _key = value;
                    EscapedKey = Escape(value);
                }
            }

            public string EscapedKey { get; private set; }

            public ulong Id { get; set; }

            public Topic Topic { get; set; }

            public static string MakeUpdatedCursor(IList<Cursor> cursors)
            {
                var sb = new StringBuilder();
                bool first = true;
                for (int i = 0; i < cursors.Count; i++)
                {
                    if (cursors[i].Id == 0)
                    {
                        continue;
                    }

                    if (!first)
                    {
                        sb.Append('|');
                    }
                    sb.Append(cursors[i].EscapedKey);
                    sb.Append(',');
                    sb.Append(cursors[i].Id);
                    first = false;
                }

                return sb.ToString();
            }

            public static string MakeCursor(IList<Cursor> cursors)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < cursors.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append('|');
                    }
                    sb.Append(cursors[i].EscapedKey);
                    sb.Append(',');
                    sb.Append(cursors[i].Id);
                }

                return sb.ToString();
            }

            private static string Escape(string value)
            {
                // Nothing to do, so bail
                if (value.IndexOfAny(_escapeChars) == -1)
                {
                    return value;
                }

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

        internal class Topic
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
            private readonly Queue<Subscription> _queue = new Queue<Subscription>();
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
                    Subscription subscription;

                    lock (_queue)
                    {
                        while (_queue.Count == 0)
                        {
                            Monitor.Wait(_queue);
                        }

                        subscription = _queue.Dequeue();
                    }

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
