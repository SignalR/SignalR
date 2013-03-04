// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Samples.Raw;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress
{
    public static class StressRuns
    {
        public static IDisposable StressGroups(int max = 100)
        {
            var host = new MemoryHost();
            host.Configure(app =>
            {
                var config = new HubConfiguration()
                {
                    Resolver = new DefaultDependencyResolver()
                };

                app.MapHubs(config);

                var configuration = config.Resolver.Resolve<IConfigurationManager>();
                // The below effectively sets the heartbeat interval to five seconds.
                configuration.KeepAlive = TimeSpan.FromSeconds(10);
            });

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
                connection.Start(new Client.Transports.LongPollingTransport(host)).Wait();

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

            return host;
        }

        public static IDisposable BrodcastFromServer()
        {
            var host = new MemoryHost();
            IHubContext context = null;

            host.Configure(app =>
            {
                var config = new HubConfiguration()
                {
                    Resolver = new DefaultDependencyResolver()
                };

                app.MapHubs(config);

                var configuration = config.Resolver.Resolve<IConfigurationManager>();
                // The below effectively sets the heartbeat interval to five seconds.
                configuration.KeepAlive = TimeSpan.FromSeconds(10);

                var connectionManager = config.Resolver.Resolve<IConnectionManager>();
                context = connectionManager.GetHubContext("EchoHub");
            });

            var cancellationTokenSource = new CancellationTokenSource();

            var thread = new Thread(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    context.Clients.All.echo();
                }
            });

            thread.Start();

            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateHubProxy("EchoHub");

            try
            {
                connection.Start(host).Wait();

                Thread.Sleep(1000);
            }
            finally
            {
                connection.Stop();
            }

            return new DisposableAction(() =>
            {
                cancellationTokenSource.Cancel();

                thread.Join();

                host.Dispose();
            });
        }

        public static IDisposable ManyUniqueGroups(int concurrency)
        {
            var host = new MemoryHost();
            var threads = new List<Thread>();
            var cancellationTokenSource = new CancellationTokenSource();

            host.Configure(app =>
            {
                var config = new HubConfiguration()
                {
                    Resolver = new DefaultDependencyResolver()
                };
                app.MapHubs(config);
            });

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

            host.Configure(app =>
            {
                var config = new HubConfiguration()
                {
                    Resolver = new DefaultDependencyResolver()
                };
                app.MapHubs(config);
            });

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

        public static IDisposable ClientGroupsSyncWithServerGroupsOnReconnectLongPolling()
        {
            var host = new MemoryHost();

            host.Configure(app =>
            {
                var config = new ConnectionConfiguration()
                {
                    Resolver = new DefaultDependencyResolver()
                };

                app.MapConnection<MyRejoinGroupConnection>("/groups", config);

                var configuration = config.Resolver.Resolve<IConfigurationManager>();
                configuration.KeepAlive = null;
                configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
            });

            var connection = new Client.Connection("http://foo/groups");
            var inGroupOnReconnect = new List<bool>();
            var wh = new ManualResetEventSlim();

            connection.Received += message =>
            {
                Console.WriteLine(message);
                wh.Set();
            };

            connection.Reconnected += () =>
            {
                connection.Send(new { type = 3, group = "test", message = "Reconnected" }).Wait();
            };

            connection.Start(new Client.Transports.LongPollingTransport(host)).Wait();

            // Join the group 
            connection.Send(new { type = 1, group = "test" }).Wait();

            Thread.Sleep(TimeSpan.FromSeconds(10));

            if (!wh.Wait(TimeSpan.FromSeconds(10)))
            {
                Debugger.Break();
            }

            Console.WriteLine(inGroupOnReconnect.Count > 0);
            Console.WriteLine(String.Join(", ", inGroupOnReconnect.Select(b => b.ToString())));

            connection.Stop();

            return host;
        }

        public static IDisposable Connect_Broadcast5msg_AndDisconnect(int concurrency)
        {
            var host = new MemoryHost();
            var threads = new List<Thread>();
            var cancellationTokenSource = new CancellationTokenSource();

            host.Configure(app =>
            {
                var config = new ConnectionConfiguration
                {
                    Resolver = new DefaultDependencyResolver()
                };
                app.MapConnection<RawConnection>("/Raw-connection", config);
            });

            for (int i = 0; i < concurrency; i++)
            {
                var thread = new Thread(_ =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        BroadcastFive(host);
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

        private static void BroadcastFive(MemoryHost host)
        {
            var connection = new Client.Connection("http://samples/Raw-connection");

            connection.Error += e =>
            {
                Console.Error.WriteLine("========ERROR==========");
                Console.Error.WriteLine(e.GetBaseException().ToString());
                Console.Error.WriteLine("=======================");
            };

            connection.Start(new Client.Transports.ServerSentEventsTransport(host)).Wait();

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    var payload = new
                    {
                        type = MessageType.Broadcast,
                        value = "message " + i.ToString()
                    };

                    connection.Send(payload).Wait();
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("========ERROR==========");
                Console.Error.WriteLine(ex.GetBaseException().ToString());
                Console.Error.WriteLine("=======================");
            }
            finally
            {
                connection.Stop();
            }
        }

        enum MessageType
        {
            Send,
            Broadcast,
            Join,
            PrivateMessage,
            AddToGroup,
            RemoveFromGroup,
            SendToGroup,
            BroadcastExceptMe,
        }

        public class MyGroupConnection : PersistentConnection
        {
            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                JObject operation = JObject.Parse(data);
                int type = operation.Value<int>("type");
                string group = operation.Value<string>("group");

                if (type == 1)
                {
                    return Groups.Add(connectionId, group);
                }
                else if (type == 2)
                {
                    return Groups.Remove(connectionId, group);
                }
                else if (type == 3)
                {
                    return Groups.Send(group, operation.Value<string>("message"));
                }

                return base.OnReceived(request, connectionId, data);
            }
        }

        public class MyRejoinGroupConnection : MyGroupConnection
        {
        }

    }
}
