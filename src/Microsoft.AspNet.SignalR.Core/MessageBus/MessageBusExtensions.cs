// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
{
    public static class MessageBusExtensions
    {
        public static Task Publish(this IMessageBus bus, string source, string key, string value)
        {
            return bus.Publish(new Message(source, key, value));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ack", Justification = "ACK is a well known networking term.")]
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
            var registration = default(CancellationTokenRegistration);

            try
            {
                registration = cancel.Register(disposer.Dispose);
            }
            catch (ObjectDisposedException)
            {
                // Dispose immediately
                disposer.Dispose();
            }

            try
            {
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
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);

                registration.Dispose();

                return tcs.Task;
            }

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
    }
}
