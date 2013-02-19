﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public static class MessageBusExtensions
    {
        public static Task Publish(this IMessageBus bus, string source, string key, string value)
        {
            if (bus == null)
            {
                throw new ArgumentNullException("bus");
            }

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            return bus.Publish(new Message(source, key, value));
        }

        internal static Task Ack(this IMessageBus bus, string connectionId, string commandId)
        {
            // Prepare the ack
            var message = new Message(connectionId, PrefixHelper.GetAck(connectionId), null);
            message.CommandId = commandId;
            message.IsAck = true;
            return bus.Publish(message);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "It's disposed in an async manner.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        internal static Task<T> Receive<T>(this IMessageBus bus,
                                           ISubscriber subscriber,
                                           string cursor,
                                           CancellationToken cancel,
                                           int maxMessages,
                                           Func<MessageResult, T> map) where T : class
        {
            var tcs = new TaskCompletionSource<T>();
            IDisposable subscription = null;

            var disposer = new Disposer();
            int resultSet = 0;
            var result = default(T);
            IDisposable registration = null;

            registration = cancel.SafeRegister(state =>
            {
                ((Disposer)state).Dispose();
            },
            disposer);

            try
            {
                subscription = bus.Subscribe(subscriber, cursor, (messageResult, state) =>
                {
                    // Mark the flag as set so we only set the result once
                    if (Interlocked.Exchange(ref resultSet, 1) == 0)
                    {
                        result = map(messageResult);
                    }

                    if (messageResult.Terminal)
                    {
                        Interlocked.CompareExchange(ref result, map(messageResult), null);

                        // Set the result
                        tcs.TrySetResult(result);

                        // Dispose of the cancellation token subscription
                        registration.Dispose();
                    }

                    return TaskAsyncHelper.False;
                },
                maxMessages,
                null);
            }
            catch (Exception ex)
            {
                tcs.TrySetUnwrappedException(ex);

                registration.Dispose();

                return tcs.Task;
            }

            // Set the disposable
            disposer.Set(subscription);

            return tcs.Task;
        }

        public static void Enumerate(this IList<ArraySegment<Message>> messages, Action<Message> onMessage)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            Enumerate<object>(messages, message => true, (state, message) => onMessage(message), state: null);
        }

        public static void Enumerate<T>(this IList<ArraySegment<Message>> messages, Func<Message, bool> filter, Action<T, Message> onMessage, T state)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            for (int i = 0; i < messages.Count; i++)
            {
                ArraySegment<Message> segment = messages[i];
                for (int j = segment.Offset; j < segment.Offset + segment.Count; j++)
                {
                    Message message = segment.Array[j];

                    if (filter(message))
                    {
                        onMessage(state, message);
                    }
                }
            }
        }
    }
}
