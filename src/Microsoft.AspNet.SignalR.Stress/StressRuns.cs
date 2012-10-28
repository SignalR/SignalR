using System;
using System.Diagnostics;
using System.Linq;
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
                host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(5);
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

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            return new DisposableAction(() => { });
        }
    }
}
