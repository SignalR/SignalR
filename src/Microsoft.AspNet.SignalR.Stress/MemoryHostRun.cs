// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Stress.Connections;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress
{
    public static class MemoryHostRun
    {
        public static IDisposable Run(int connections, int senders, string payload, string transport)
        {
            var host = new MemoryHost();

            host.Configure(app =>
            {
                app.MapConnection<StressConnection>("/echo");
            });

            var countDown = new CountdownEvent(senders);
            var cancellationTokenSource = new CancellationTokenSource();

            for (int i = 0; i < connections; i++)
            {
                if (transport.Equals("longPolling", StringComparison.OrdinalIgnoreCase))
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        string connectionId = state.ToString();
                        LongPollingLoop(host, connectionId);

                    }, i);
                }
                else
                {
                    string connectionId = i.ToString();
                    ProcessRequest(host, transport, connectionId);
                }
            }

            for (var i = 0; i < senders; i++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        string connectionId = i.ToString();
                        ProcessSendRequest(host, transport, connectionId, payload);
                    }

                    countDown.Signal();
                });
            }

            return new DisposableAction(() =>
            {
                cancellationTokenSource.Cancel();

                // Wait for all senders to stop
                countDown.Wait(TimeSpan.FromMilliseconds(1000 * senders));

                host.Dispose();
            });
        }

        private static Task ProcessRequest(MemoryHost host, string transport, string connectionId)
        {
            return host.ProcessRequest("http://foo/echo/connect?transport=" + transport + "&connectionId=" + connectionId, request => { }, null, disableWrites: true);
        }

        private static Task ProcessSendRequest(MemoryHost host, string transport, string connectionId, string data)
        {
            var postData = new Dictionary<string, string> { { "data", data } };
            return host.ProcessRequest("http://foo/echo/send?transport=" + transport + "&connectionId=" + connectionId, request => { }, postData);
        }

        private static void LongPollingLoop(MemoryHost host, string connectionId)
        {
        LongPoll:

            var task = ProcessRequest(host, "longPolling", connectionId);

            if (task.IsCompleted)
            {
                task.Wait();

                goto LongPoll;
            }

            task.ContinueWith(t => LongPollingLoop(host, connectionId));
        }
    }
}
