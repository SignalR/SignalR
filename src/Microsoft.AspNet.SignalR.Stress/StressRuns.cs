using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    public static class StressRuns
    {
        public static IDisposable StressGroups(int max = 100)
        {
            using (var host = new MemoryHost())
            {
                host.HubPipeline.EnableAutoRejoiningGroups();
                host.MapHubs();
                host.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(5);
                host.Configuration.KeepAlive = TimeSpan.FromSeconds(5);

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                var connection = new Client.Hubs.HubConnection("http://foo");
                var proxy = connection.CreateHubProxy("HubWithGroups");

                proxy.On<int>("Do", i =>
                {
                    if (!countDown.Mark(i))
                    {
                        Debugger.Break();
                    }
                });

                try
                {
                    connection.Start(host).Wait();

                    proxy.Invoke("Join", "foo").Wait();

                    for (int i = 0; i < max; i++)
                    {
                        proxy.Invoke("Send", "foo", i).Wait();
                    }

                    proxy.Invoke("Leave", "foo").Wait();

                    for (int i = max + 1; i < max + 50; i++)
                    {
                        proxy.Invoke("Send", "foo", i).Wait();
                    }

                    if (!countDown.Wait(TimeSpan.FromSeconds(10)))
                    {
                        Console.WriteLine("Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
                        Debugger.Break();
                    }
                }
                finally
                {
                    connection.Stop();
                }
            }

            return new DisposableAction(() => { });
        }

        public static IDisposable ManyUniqueGroups(int concurrency)
        {
            var host = new MemoryHost();
            var threads = new List<Thread>();
            var cancellationTokenSource = new CancellationTokenSource();

            host.MapHubs();

            for (int i = 0; i < concurrency; i++)
            {
                var thread = new Thread(_ =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        RunOne(host);
                    }
                });

                threads.Add(thread);
                thread.Start();
            }

            return new DisposableAction(() =>
            {
                cancellationTokenSource.Cancel();

                threads.ForEach(t => t.Join());

                host.Dispose();
            });
        }

        private static void RunOne(MemoryHost host)
        {
            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateHubProxy("HubWithGroups");
            var wh = new ManualResetEventSlim(false);

            proxy.On<int>("Do", i => wh.Set());

            try
            {
                connection.Start(host).Wait();

                proxy.Invoke("Join", "foo").Wait();

                proxy.Invoke("Send", "foo", 0).Wait();

                if (!wh.Wait(TimeSpan.FromSeconds(10)))
                {
                    Debugger.Break();
                }
            }
            finally
            {
                connection.Stop();
            }
        }


        public static IDisposable RunConnectDisconnect(int connections)
        {
            var host = new MemoryHost();

            host.MapHubs();

            for (int i = 0; i < connections; i++)
            {
                var connection = new Client.Hubs.HubConnection("http://foo");
                var proxy = connection.CreateHubProxy("EchoHub");
                var wh = new ManualResetEventSlim(false);

                proxy.On("echo", _ => wh.Set());

                try
                {
                    connection.Start(host).Wait();

                    proxy.Invoke("Echo", "foo").Wait();

                    if (!wh.Wait(TimeSpan.FromSeconds(10)))
                    {
                        Debugger.Break();
                    }
                }
                finally
                {
                    connection.Stop();
                }
            }

            return host;
        }
    }
}
