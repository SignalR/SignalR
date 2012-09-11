using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR
{
    public static class MessageBusExtensions
    {
        public static Task Publish(this IMessageBus bus, string source, string key, string value)
        {
            return bus.Publish(new Message(source, key, value));
        }

        public static Task ReceiveAck(this IMessageBus bus,
                                      Message message,
                                      CancellationToken cancel)
        {
            if (message.IsCommand)
            {
                // Only supported for commands
                return ReceiveAck(bus, message.Source, message.Source, message.CommandId, cancel);
            }

            return TaskAsyncHelper.Empty;
        }

        private static Task ReceiveAck(IMessageBus bus,
                                       string source,
                                       string eventKey,
                                       string commandId,
                                       CancellationToken cancel)
        {
            var tcs = new TaskCompletionSource<object>();

            // Subscribe to the "ack" event so that we can get a reply for the command id specified
            var eventKeys = new[] { AckPrefix(eventKey) };

            // We don't care about the task returned from this since we're looking for a specific message
            bus.ReceiveAsync<object>(new Subscriber(eventKeys, source),
                                     cursor: null,
                                     cancel: cancel,
                                     maxMessages: Int32.MaxValue,
                                     map: result =>
                                     {
                                         // Set the result if we have the response for the specified command id
                                         result.Messages.Enumerate(m => m.IsAck && m.CommandId.Equals(commandId),
                                                                   m => tcs.TrySetResult(null));
                                         return null;
                                     },
                                     end: (result, obj) => { });

            return tcs.Task;
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
                                              Action<MessageResult, T> end)
        {
            var tcs = new TaskCompletionSource<T>();
            IDisposable subscription = null;

            const int stateUnassigned = 0;
            const int stateAssigned = 1;
            const int stateDisposed = 2;

            int state = stateUnassigned;
            int resultSet = 0;
            var result = default(T);

            CancellationTokenRegistration registration = cancel.Register(() =>
            {
                // Dispose the subscription only if the handle has been assigned. If not, flag it so that the subscriber knows to Dispose of it for use
                if (Interlocked.Exchange(ref state, stateDisposed) == stateAssigned)
                {
                    subscription.Dispose();
                }
            });

            subscription = bus.Subscribe(subscriber, cursor, messageResult =>
            {
                // Mark the flag as set so we only set the result once
                if (Interlocked.Exchange(ref resultSet, 1) == 0)
                {
                    // Dispose of the cancellation token subscription
                    registration.Dispose();

                    // Get the result
                    result = map(messageResult);

                    // Dispose the subscription only if the handle has been assigned. If not, flag it so that the subscriber knows to Dispose of it for use
                    if (Interlocked.Exchange(ref state, stateDisposed) == stateAssigned)
                    {
                        subscription.Dispose();
                    }
                }

                if (messageResult.Terminal)
                {
                    // Fire a callback before the result is set
                    end(messageResult, result);

                    // Set the result
                    tcs.TrySetResult(result);

                    return TaskAsyncHelper.False;
                }

                return TaskAsyncHelper.True;
            },
            maxMessages);

            // If callbacks have already run, they maybe have not been able to Dispose the subscription because the instance was not yet assigned
            if (Interlocked.Exchange(ref state, stateAssigned) == stateDisposed)
            {
                // In this case, we will dispose of it immediately.
                subscription.Dispose();
            }

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
