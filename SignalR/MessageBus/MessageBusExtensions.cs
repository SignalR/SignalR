using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public static class MessageBusExtensions
    {
        public static Task Publish(this IMessageBus bus, string source, string key, string value)
        {
            return bus.Publish(new Message(source, key, value));
        }

        public static Task Ack(this IMessageBus bus, string source, string eventKey, string commandId)
        {
            // Prepare the ack
            var message = new Message(source, AckPrefix(eventKey), null);
            message.CommandId = commandId;
            message.IsAck = true;
            return bus.Publish(message);
        }

        public static Task<T> ReceiveAsync<T>(this IMessageBus bus,
                                              ISubscriber subscriber,
                                              string cursor,
                                              CancellationToken cancel,
                                              int maxMessages,
                                              Func<MessageResult, T> map,
                                              Action<MessageResult, T> end) where T : class
        {
            var tcs = new TaskCompletionSource<T>();
            IDisposable subscription = null;

            var disposer = new Disposer();
            int resultSet = 0;
            var result = default(T);

            CancellationTokenRegistration registration = cancel.Register(disposer.Dispose);

            subscription = bus.Subscribe(subscriber, cursor, messageResult =>
            {
                // Mark the flag as set so we only set the result once
                if (Interlocked.Exchange(ref resultSet, 1) == 0)
                {
                    result = map(messageResult);

                    // Dispose of the cancellation token subscription
                    registration.Dispose();

                    // Dispose the subscription
                    disposer.Dispose();
                }

                if (messageResult.Terminal)
                {
                    Interlocked.CompareExchange(ref result, map(messageResult), null);

                    // Fire a callback before the result is set
                    end(messageResult, result);

                    // Set the result
                    tcs.TrySetResult(result);
                }

                return TaskAsyncHelper.False;
            },
            maxMessages);

            // Set the disposable
            disposer.Set(subscription);

            return tcs.Task;
        }

        public static void Enumerate(this IList<ArraySegment<Message>> messages, Action<Message> onMessage)
        {
            Enumerate(messages, message => true, onMessage);
        }

        public static void Enumerate(this IList<ArraySegment<Message>> messages, Func<Message, bool> filter, Action<Message> onMessage)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                ArraySegment<Message> segment = messages[i];
                for (int j = segment.Offset; j < segment.Offset + segment.Count; j++)
                {
                    Message message = segment.Array[j];

                    if (filter(message))
                    {
                        onMessage(message);
                    }
                }
            }
        }

        private static string AckPrefix(string eventKey)
        {
            return "ACK_" + eventKey;
        }

        private class Subscriber : ISubscriber
        {
            public IEnumerable<string> EventKeys
            {
                get;
                private set;
            }

            public string Identity
            {
                get;
                private set;
            }

            public event Action<string> EventAdded;

            public event Action<string> EventRemoved;

            public Subscriber(IEnumerable<string> eventKeys, string source)
            {
                EventKeys = eventKeys;
                Identity = source;
            }
        }
    }
}
