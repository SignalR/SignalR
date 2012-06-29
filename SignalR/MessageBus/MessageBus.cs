using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        private const int DefaultMaxStackDepth = 1000;
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

        /// <summary>
        /// Publishes a new message to the specified event on the bus.
        /// </summary>
        /// <param name="source">A value representing the source of the data sent.</param>
        /// <param name="eventKey">The specific event key to send data to.</param>
        /// <param name="value">The value to send.</param>
        public void Publish(string source, string eventKey, object value)
        {
            var topic = _topics.GetOrAdd(eventKey, _ => new Topic());

            topic.Store.Add(new Message(eventKey, value));

            foreach (var subscription in topic.Subscriptions.GetSnapshot())
            {
                _engine.Schedule(subscription);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="cursor"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IEnumerable<string> keys, string cursor, Func<Exception, MessageResult, Task> callback)
        {
            var subscription = new Subscription
            {
                Cursors = GetCursors(cursor, keys),
                Callback = callback
            };

            foreach (var key in keys)
            {
                var topic = _topics.GetOrAdd(key, _ => new Topic());
                topic.Subscriptions.Add(subscription);
            }

            return new DisposableAction(() =>
            {
                foreach (var key in keys)
                {
                    Topic topic;
                    if (_topics.TryGetValue(key, out topic))
                    {
                        topic.Subscriptions.Remove(subscription);
                    }
                }

                string currentCursor = Subscription.MakeCursor(subscription.Cursors);
                subscription.Callback.Invoke(null, new MessageResult(currentCursor));
            });
        }

        private Cursor[] GetCursors(string messageId, IEnumerable<string> keys)
        {
            if (messageId == null)
            {
                return keys.Select(key => new Cursor { Key = key, MessageId = GetMessageId(key) }).ToArray();
            }

            return JsonConvert.DeserializeObject<Cursor[]>(messageId);
        }

        private ulong GetMessageId(string key)
        {
            Topic topic;
            if (_topics.TryGetValue(key, out topic))
            {
                return topic.Store.Id + 1;
            }

            return 0;
        }

        private class Subscription
        {
            public Cursor[] Cursors;
            public Func<Exception, MessageResult, Task> Callback;

            public bool Queued { get; set; }
            public bool Working { get; private set; }

            public Task Work(ConcurrentDictionary<string, Topic> topics)
            {
                if (Working)
                {
                    return TaskAsyncHelper.Empty;
                }

                var tcs = new TaskCompletionSource<object>();

                Working = true;
                WorkImpl(topics, tcs);

                // Fast Path
                if (tcs.Task.Status == TaskStatus.RanToCompletion)
                {
                    Working = false;
                    return tcs.Task;
                }

                return FinishAsync(tcs);
            }

            private Task FinishAsync(TaskCompletionSource<object> tcs)
            {
                return tcs.Task.ContinueWith(task =>
                {
                    Working = false;

                    if (task.IsFaulted)
                    {
                        return TaskAsyncHelper.FromError(task.Exception);
                    }

                    return TaskAsyncHelper.Empty;
                },
                TaskContinuationOptions.OnlyOnRanToCompletion).FastUnwrap();
            }

            private void WorkImpl(ConcurrentDictionary<string, Topic> topics, TaskCompletionSource<object> taskCompletionSource)
            {
                try
                {
                Process:
                    var results = new List<ResultSet>();
                    foreach (var cursor in Cursors)
                    {
                        Topic topic;
                        if (topics.TryGetValue(cursor.Key, out topic))
                        {
                            var result = new ResultSet
                            {
                                Cursor = cursor,
                                Messages = new List<Message>()
                            };

                            MessageStoreResult<Message> storeResult = topic.Store.GetMessages(cursor.MessageId);
                            ulong next = storeResult.FirstMessageId + (ulong)storeResult.Messages.Length;
                            cursor.MessageId = next;

                            if (storeResult.Messages.Length > 0)
                            {
                                result.Messages.AddRange(storeResult.Messages);
                                results.Add(result);
                            }
                        }
                    }

                    if (results.Count > 0)
                    {
                        MessageResult messageResult = GetMessageResult(results);
                        Task task = Callback.Invoke(null, messageResult);

                        if (task.Status == TaskStatus.RanToCompletion)
                        {
                            // Sync path
                            goto Process;
                        }
                        else
                        {
                            // Async path
                            task.Then((top, tcs) => WorkImpl(top, tcs), topics, taskCompletionSource)
                                .Catch(ex => taskCompletionSource.TrySetException(ex));
                        }
                    }
                    else
                    {
                        taskCompletionSource.TrySetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            }

            private static MessageResult GetMessageResult(List<ResultSet> results)
            {
                var messages = results.SelectMany(r => r.Messages).ToArray();
                return new MessageResult(messages, MakeCursor(results));
            }

            private static string MakeCursor(List<ResultSet> results)
            {
                return MakeCursor(results.Select(r => r.Cursor));
            }

            public static string MakeCursor(IEnumerable<Cursor> cursors)
            {
                return JsonConvert.SerializeObject(cursors);
            }
        }

        private class Cursor
        {
            [JsonProperty(PropertyName = "k")]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "m")]
            public ulong MessageId { get; set; }
        }

        private struct ResultSet
        {
            public Cursor Cursor;
            public List<Message> Messages;
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

        private class Engine
        {
            private readonly BlockingCollection<Subscription> _queue = new BlockingCollection<Subscription>();
            private readonly ConcurrentDictionary<string, Topic> _topics = new ConcurrentDictionary<string, Topic>();

            private const int MaxLimit = 10;
            private const int IdleLimit = 5;

            private int _allocatedWorkers;
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

            public void Schedule(Subscription subscription)
            {
                if (subscription.Queued)
                {
                    return;
                }

                subscription.Queued = true;
                _queue.TryAdd(subscription);
                AddWorker();
            }

            public void AddWorker()
            {
                // Only create a new worker if everyone is busy (up to the max)
                if (_allocatedWorkers < MaxLimit && _allocatedWorkers == _busyWorkers)
                {
                    _allocatedWorkers++;
                    Trace.TraceInformation("Creating a worker, allocated={0}, busy={1}", _allocatedWorkers, _busyWorkers);
                    ThreadPool.QueueUserWorkItem(ProcessWork);
                }
            }

            private void ProcessWork(object state)
            {
                Pump().ContinueWith(task =>
                {
                    _allocatedWorkers--;

                    if (task.IsFaulted)
                    {
                        Trace.TraceInformation("Failed to process work - " + task.Exception.GetBaseException());
                    }
                },
                TaskContinuationOptions.OnlyOnRanToCompletion);
            }

            public Task Pump()
            {
                var tcs = new TaskCompletionSource<object>();
                PumpImpl(tcs);
                return tcs.Task;
            }

            public void PumpImpl(TaskCompletionSource<object> taskCompletionSource)
            {

            Process:
                // If we're withing the acceptable limit of idleness, just keep running
                int idleWorkers = _allocatedWorkers - _busyWorkers;
                if (idleWorkers <= IdleLimit)
                {
                    Subscription subscription = _queue.Take();

                    _busyWorkers++;
                    try
                    {
                        Task task = subscription.Work(_topics);

                        if (task.Status == TaskStatus.RanToCompletion)
                        {
                            // Sync path
                            _busyWorkers--;
                            subscription.Queued = false;

                            goto Process;
                        }
                        else
                        {
                            // Async path
                            task.Then((tcs, sub) =>
                            {
                                _busyWorkers--;
                                sub.Queued = false;

                                PumpImpl(tcs);
                            },
                            taskCompletionSource,
                            subscription)
                            .Catch(ex =>
                            {
                                taskCompletionSource.TrySetException(ex);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                }
                else
                {
                    Trace.TraceInformation("Idle workers are {0}", idleWorkers);
                    taskCompletionSource.TrySetResult(null);
                }
            }
        }
    }
}
