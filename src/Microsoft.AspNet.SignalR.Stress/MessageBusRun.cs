// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    public static class MessageBusRun
    {
        public static IDisposable Run(int connections, int senders, string payload, int messageBufferSize = 10)
        {
            var resolver = new DefaultDependencyResolver();
            var bus = new MessageBus(resolver);
            var countDown = new CountdownEvent(senders);
            var subscriptions = new List<IDisposable>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Initialize performance counters for this run
            Utility.InitializePerformanceCounters(resolver, cancellationTokenSource.Token);

            for (int i = 0; i < connections; i++)
            {
                string identity = i.ToString();
                var subscriber = new Subscriber(identity, new[] { "a", "b", "c" });
                IDisposable subscription = bus.Subscribe(subscriber,
                                                         cursor: null,
                                                         callback: _ => TaskAsyncHelper.True,
                                                         maxMessages: messageBufferSize);

                subscriptions.Add(subscription);
            }

            for (var i = 0; i < senders; i++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        string source = i.ToString();
                        bus.Publish(source, "a", payload);
                    }

                    countDown.Signal();
                });
            }

            return new DisposableAction(() =>
            {
                cancellationTokenSource.Cancel();

                // Wait for all senders to stop
                countDown.Wait(TimeSpan.FromMilliseconds(1000 * senders));

                // Shut the bus down and wait for workers to stop
                bus.Dispose();

                // Dispose of all the subscriptions
                subscriptions.ForEach(s => s.Dispose());
            });
        }
    }
}
