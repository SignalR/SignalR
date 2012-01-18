using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class InProcessMessageBus
    {
        private static List<InProcessMessage> _emptyMessageList = new List<InProcessMessage>();

        private readonly ConcurrentDictionary<string, LockedList<Action<IEnumerable<Message>>>> _waitingTasks =
            new ConcurrentDictionary<string, LockedList<Action<IEnumerable<Message>>>>();

        private readonly ConcurrentDictionary<string, LockedList<InProcessMessage>> _cache =
            new ConcurrentDictionary<string, LockedList<InProcessMessage>>();

        private readonly object _messageCreationLock = new object();

        private ulong _lastMessageId = 0;

        public Task<IEnumerable<Message>> GetMessagesSince(IEnumerable<string> eventKeys, ulong? id = null)
        {
            if (id == null)
            {
                // Wait for new messages
                return WaitForMessages(eventKeys);
            }

            var messages = eventKeys.SelectMany(key => GetMessagesSince(key, id.Value));
            if (messages.Any())
            {
                // Messages already in store greater than last received id so return them
                return TaskAsyncHelper.FromResult((IEnumerable<Message>)messages.OrderBy(msg => msg.MessageId));
            }

            // Wait for new messages
            return WaitForMessages(eventKeys);
        }

        public Task Send(string eventKey, object value)
        {
            var list = _cache.GetOrAdd(eventKey, _ => new LockedList<InProcessMessage>());

            InProcessMessage message = null;
            lock (_messageCreationLock)
            {
                // Only 1 save allowed at a time, to ensure messages are added to the list in order
                message = new InProcessMessage(eventKey, GenerateId(), value);
                list.Add(message);
            }

            // Send to waiting callers
            LockedList<Action<IEnumerable<Message>>> taskCompletionSources;
            if (_waitingTasks.TryGetValue(eventKey, out taskCompletionSources))
            {
                var delegates = taskCompletionSources.Copy();
                var messages = new[] { message };

                foreach (var callback in delegates)
                {
                    if (callback != null)
                    {
                        callback.Invoke(messages);
                    }
                }
            }

            return TaskAsyncHelper.Empty;
        }

        private ulong GenerateId()
        {
            // do some crazy shit here
            return ++_lastMessageId;
        }

        private IEnumerable<InProcessMessage> GetMessagesSince(string eventKey, ulong id)
        {
            LockedList<InProcessMessage> store;
            List<InProcessMessage> list = null;
            if (_cache.TryGetValue(eventKey, out store))
            {
                list = store.Copy();
            }

            if (list == null || list.Count == 0)
            {
                return _emptyMessageList;
            }

            if (list.Count > 0 && list[0].MessageId > id)
            {
                // All messages in the list are greater than the last message
                return list;
            }

            var index = list.FindLastIndex(msg => msg.MessageId <= id);

            if (index < 0)
            {
                return _emptyMessageList;
            }

            var startIndex = index + 1;

            if (startIndex >= list.Count)
            {
                return _emptyMessageList;
            }

            return list.GetRange(startIndex, list.Count - startIndex);
        }

        private Task<IEnumerable<Message>> WaitForMessages(IEnumerable<string> eventKeys)
        {
            var tcs = new TaskCompletionSource<IEnumerable<Message>>();
            int callbackCalled = 0;
            Action<IEnumerable<Message>> callback = null;

            callback = messages =>
            {
                if (Interlocked.Exchange(ref callbackCalled, 1) == 0)
                {
                    tcs.SetResult(messages);
                }

                foreach (var eventKey in eventKeys)
                {
                    LockedList<Action<IEnumerable<Message>>> callbacks;
                    if (_waitingTasks.TryGetValue(eventKey, out callbacks))
                    {
                        callbacks.Remove(callback);
                    }
                }
            };

            foreach (var eventKey in eventKeys)
            {
                var handlers = _waitingTasks.GetOrAdd(eventKey, _ => new LockedList<Action<IEnumerable<Message>>>());
                handlers.Add(callback);
            }

            return tcs.Task;
        }

        private class InProcessMessage : Message
        {
            public ulong MessageId { get; set; }

            public InProcessMessage(string signalKey, ulong id, object value)
                : base(signalKey, id.ToString(CultureInfo.InvariantCulture), value)
            {
                MessageId = id;
            }
        }
    }
}