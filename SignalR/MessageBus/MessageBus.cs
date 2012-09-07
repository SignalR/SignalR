using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        public Task Publish(Message message)
        {
            Topic topic = _topics.GetOrAdd(message.Key, _ => new Topic());

            topic.Store.Add(message);

            // TODO: Consider a ReaderWriterLockSlim (or use our LockedList<T>)
            lock (topic.Subscriptions)
            {
                for (int i = 0; i < topic.Subscriptions.Count; i++)
                {
                    Subscription subscription = topic.Subscriptions[i];
                    _engine.Schedule(subscription);
                }
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
        public IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
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

            var subscription = new Subscription(subscriber.Identity, cursors, callback, messageBufferSize);
            var topics = new HashSet<Topic>();

            foreach (var key in subscriber.EventKeys)
            {
                Topic topic = _topics.GetOrAdd(key, _ => new Topic());

                // Set the subscription for this topic
                subscription.SetCursorTopic(key, topic);

                // Add it to the list of topics
                topics.Add(topic);
            }

            foreach (var topic in topics)
            {
                topic.AddSubscription(subscription);
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
                subscription.AddOrUpdateCursor(eventKey, id, topic);

                // Add it to the list of subs
                topic.AddSubscription(subscription);
            };

            Action<string> eventRemoved = eventKey => RemoveEvent(subscription, eventKey);

            subscriber.EventAdded += eventAdded;
            subscriber.EventRemoved += eventRemoved;

            // If there's a cursor then schedule work for this subscription
            if (!String.IsNullOrEmpty(cursor))
            {
                _engine.Schedule(subscription);
            }

            return new DisposableAction(() =>
            {
                // This will stop work from continuting to happen
                subscription.Dispose();

                subscriber.EventAdded -= eventAdded;
                subscriber.EventRemoved -= eventRemoved;

                string currentCursor = Cursor.MakeCursor(subscription.Cursors);

                foreach (var eventKey in subscriber.EventKeys)
                {
                    RemoveEvent(subscription, eventKey);
                }

                subscription.Invoke(new MessageResult(currentCursor));
            });
        }

        private void RemoveEvent(Subscription subscription, string eventKey)
        {
            Topic topic;
            if (_topics.TryGetValue(eventKey, out topic))
            {
                topic.RemoveSubscription(subscription);
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

        internal class Subscription : IDisposable
        {
            private readonly List<Cursor> _cursors;
            private readonly Func<MessageResult, Task<bool>> _callback;
            private readonly int _maxMessages;

            private readonly object _lockObj = new object();
            private int _disposed;

            private int _queued;
            private int _working;

            private bool Alive
            {
                get
                {
                    return _disposed == 0;
                }
            }

            public IList<Cursor> Cursors
            {
                get
                {
                    return _cursors;
                }
            }

            public string Identity { get; private set; }

            public Subscription(string identity, IEnumerable<Cursor> cursors, Func<MessageResult, Task<bool>> callback, int maxMessages)
            {
                Identity = identity;
                _cursors = new List<Cursor>(cursors);
                _callback = callback;
                _maxMessages = maxMessages;
            }

            public Task<bool> Invoke(MessageResult result)
            {
                return _callback.Invoke(result);
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
                int totalCount = 0;
                string nextCursor = null;
                List<ArraySegment<Message>> items = null;

                lock (_lockObj)
                {
                    items = new List<ArraySegment<Message>>(Cursors.Count);
                    for (int i = 0; i < Cursors.Count; i++)
                    {
                        Cursor cursor = Cursors[i];

                        MessageStoreResult<Message> storeResult = cursor.Topic.Store.GetMessages(cursor.Id, _maxMessages);
                        ulong next = storeResult.FirstMessageId + (ulong)storeResult.Messages.Count;
                        cursor.Id = next;

                        if (storeResult.Messages.Count > 0)
                        {
                            items.Add(storeResult.Messages);
                            totalCount += storeResult.Messages.Count;
                        }
                    }

                    nextCursor = Cursor.MakeCursor(Cursors);
                }

                if (Alive && items.Count > 0)
                {
                    var messageResult = new MessageResult(items, nextCursor, totalCount);
                    Task<bool> callbackTask = Invoke(messageResult);

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
                                // If the callback said it's done then stop
                                taskCompletionSource.TrySetResult(null);
                            }
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

            private void WorkImplAsync(Task<bool> callbackTask, ConcurrentDictionary<string, Topic> topics, TaskCompletionSource<object> taskCompletionSource)
            {
                // Async path
                callbackTask.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        taskCompletionSource.TrySetException(task.Exception);
                    }
                    else if (task.Result)
                    {
                        WorkImpl(topics, taskCompletionSource);
                    }
                    else
                    {
                        // If the callback said it's done then stop
                        taskCompletionSource.TrySetResult(null);
                    }
                });
            }

            public void AddOrUpdateCursor(string key, ulong id, Topic topic)
            {
                lock (_lockObj)
                {
                    // O(n), but small n and it's not common
                    var index = _cursors.FindIndex(c => c.Key == key);
                    if (index == -1)
                    {
                        _cursors.Add(new Cursor
                        {
                            Key = key,
                            Id = id,
                            Topic = topic
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

            public void Dispose()
            {
                // REVIEW: Should we make this block if there's pending work
                Interlocked.Exchange(ref _disposed, 1);
            }

            public override int GetHashCode()
            {
                return Identity.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Identity.Equals(((Subscription)obj).Identity);
            }
        }

        internal unsafe class Cursor
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

            public static string MakeCursor(IList<Cursor> cursors)
            {
                return MakeCursorFast(cursors) ?? MakeCursorSlow(cursors);
            }

            private static string MakeCursorSlow(IList<Cursor> cursors)
            {
                var serialized = new string[cursors.Count];
                for (int i = 0; i < cursors.Count; i++)
                {
                    serialized[i] = cursors[i].EscapedKey + ',' + cursors[i].Id;
                }

                return String.Join("|", serialized);
            }

            private static string MakeCursorFast(IList<Cursor> cursors)
            {
                const int MAX_CHARS = 8 * 1024;
                char* pChars = stackalloc char[MAX_CHARS];
                char* pNextChar = pChars;
                int numCharsInBuffer = 0;

                // Start shoving data into the buffer
                for (int i = 0; i < cursors.Count; i++)
                {
                    Cursor cursor = cursors[i];
                    string escapedKey = cursor.EscapedKey;

                    checked
                    {
                        numCharsInBuffer += escapedKey.Length + 18; // comma + 16-char hex Id + pipe
                    }

                    if (numCharsInBuffer > MAX_CHARS)
                    {
                        return null; // we will overrun the buffer
                    }

                    for (int j = 0; j < escapedKey.Length; j++)
                    {
                        *pNextChar++ = escapedKey[j];
                    }

                    *pNextChar = ',';
                    pNextChar++;
                    WriteUlongAsHexToBuffer(cursor.Id, pNextChar);
                    pNextChar += 16;
                    *pNextChar = '|';
                    pNextChar++;
                }

                return (numCharsInBuffer == 0) ? String.Empty : new String(pChars, 0, numCharsInBuffer - 1); // -1 for final pipe
            }

            private static void WriteUlongAsHexToBuffer(ulong value, char* pBuffer)
            {
                for (int i = 15; i >= 0; i--)
                {
                    pBuffer[i] = Int32ToHex((int)value & 0xf); // don't care about overflows here
                    value >>= 4;
                }
            }

            private static char Int32ToHex(int value)
            {
                return (value < 10) ? (char)(value + '0') : (char)(value - 10 + 'A');
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
                            current.Id = UInt64.Parse(sb.ToString(), NumberStyles.HexNumber);
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
                    current.Id = UInt64.Parse(sb.ToString(), NumberStyles.HexNumber);
                    cursors.Add(current);
                }

                return cursors.ToArray();
            }

            public override string ToString()
            {
                return Key;
            }
        }

        internal class Topic
        {
            private HashSet<string> _subs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public IList<Subscription> Subscriptions { get; private set; }
            public MessageStore<Message> Store { get; private set; }

            public Topic()
            {
                Subscriptions = new List<Subscription>();
                Store = new MessageStore<Message>(DefaultMessageStoreSize);
            }

            public void AddSubscription(Subscription subscription)
            {
                lock (Subscriptions)
                {
                    if (_subs.Add(subscription.Identity))
                    {
                        Subscriptions.Add(subscription);
                    }
                }
            }

            public void RemoveSubscription(Subscription subscription)
            {
                lock (Subscriptions)
                {
                    if (_subs.Remove(subscription.Identity))
                    {
                        Subscriptions.Remove(subscription);
                    }
                }
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

            private static readonly TimeSpan _idleTimeout = TimeSpan.FromSeconds(30);

            // The maximum number of workers (threads) allowed to process all incoming messages
            private static readonly int MaxWorkers = 3 * Environment.ProcessorCount;

            // The maximum number of workers that can be left to idle (not busy but allocated)
            private static readonly int MaxIdleWorkers = Environment.ProcessorCount;

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
