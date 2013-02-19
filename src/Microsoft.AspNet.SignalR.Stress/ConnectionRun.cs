// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Stress.Connections;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;

namespace Microsoft.AspNet.SignalR.Stress
{
    public class ConnectionRun
    {
        internal static IDisposable ReceiveLoopRun(int connections, int senders, string payload)
        {
            var resolver = new DefaultDependencyResolver();
            var connectionManager = new ConnectionManager(resolver);
            var subscriptions = new List<IDisposable>();
            var senderCountDown = new CountdownEvent(senders);
            var connectionCountDown = new CountdownEvent(connections);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var context = connectionManager.GetConnectionContext<StressConnection>();

            // Initialize performance counters for this run
            Utility.InitializePerformanceCounters(resolver, cancellationTokenSource.Token);

            for (int i = 0; i < connections; i++)
            {
                var transportConnection = (ITransportConnection)context.Connection;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    ReceiveLoop(connectionCountDown,
                                transportConnection,
                                messageId: null,
                                cancellationToken: cancellationToken);
                });

            }

            for (var i = 1; i <= senders; i++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        context.Connection.Broadcast(payload);
                    }

                    senderCountDown.Signal();
                });
            }

            return new DisposableAction(() =>
            {
                cancellationTokenSource.Cancel();

                // Wait for each loop
                connectionCountDown.Wait(TimeSpan.FromMilliseconds(500 * connections));

                // Wait for all senders to stop
                senderCountDown.Wait(TimeSpan.FromMilliseconds(1000 * senders));

                subscriptions.ForEach(s => s.Dispose());
            });
        }

        private static void ReceiveLoop(CountdownEvent connectionCountDown, ITransportConnection connection, string messageId, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                connectionCountDown.Signal();
                return;
            }

            connection.Receive(messageId, cancellationToken, maxMessages: 5000).Then(r =>
            {
                ReceiveLoop(connectionCountDown, connection, r.MessageId, cancellationToken);
            });
        }

        internal static IDisposable LongRunningSubscriptionRun(int connections, int senders, string payload)
        {
            var resolver = new DefaultDependencyResolver();
            var connectionManager = new ConnectionManager(resolver);
            var subscriptions = new List<IDisposable>();
            var countDown = new CountdownEvent(senders);
            var cancellationTokenSource = new CancellationTokenSource();
            var context = connectionManager.GetConnectionContext<StressConnection>();

            // Initialize performance counters for this run
            Utility.InitializePerformanceCounters(resolver, cancellationTokenSource.Token);

            for (int i = 0; i < connections; i++)
            {
                var transportConnection = (ITransportConnection)context.Connection;
                var subscription = transportConnection.Receive(messageId: null,
                                                               callback: (_, __) => TaskAsyncHelper.True,
                                                               maxMessages: 10,
                                                               state: null);
                subscriptions.Add(subscription);
            }

            for (var i = 1; i <= senders; i++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        context.Connection.Broadcast(payload);
                    }

                    countDown.Signal();
                });
            }

            return new DisposableAction(() =>
            {
                cancellationTokenSource.Cancel();

                // Wait for all senders to stop
                countDown.Wait(TimeSpan.FromMilliseconds(1000 * senders));

                subscriptions.ForEach(s => s.Dispose());
            });
        }
    }
}
