// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Newtonsoft.Json.Linq;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress
{
    public static class StressRuns
    {
        static StressRuns()
        {
            // HACK: This is horrible but we need the assembly to be loaded
            // so that hubs are detected
            Assembly.Load("Microsoft.AspNet.SignalR.StressServer");
        }

        public static IDisposable StressGroups(int max = 100)
        {
            var host = new MemoryHost();
            host.Configure(app =>
            {
                var config = new HubConfiguration()
                {
                    Resolver = new DefaultDependencyResolver()
                };

                app.MapSignalR(config);

                var configuration = config.Resolver.Resolve<IConfigurationManager>();
                // The below effectively sets the heartbeat interval to five seconds.
                configuration.KeepAlive = TimeSpan.FromSeconds(10);
            });

            var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
            var connection = new HubConnection("http://foo");
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

                app.MapSignalR(config);

                var configuration = config.Resolver.Resolve<IConfigurationManager>();
                // The below effectively sets the heartbeat interval to five seconds.
                configuration.KeepAlive = TimeSpan.FromSeconds(10);

                var connectionManager = config.Resolver.Resolve<IConnectionManager>();
                context = connectionManager.GetHubContext("SimpleEchoHub");
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

            var connection = new HubConnection("http://foo");
            var proxy = connection.CreateHubProxy("SimpleEchoHub");

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
                app.MapSignalR(config);
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
            var connection = new HubConnection("http://foo");
            var proxy = connection.CreateHubProxy("OnConnectedOnDisconnectedHub");

            try
            {
                connection.Start(host).Wait();

                string guid = Guid.NewGuid().ToString();
                string otherGuid = proxy.Invoke<string>("Echo", guid).Result;

                if (!guid.Equals(otherGuid))
                {
                    throw new InvalidOperationException("Fail!");
                }
            }
            finally
            {
                connection.Stop();
            }
        }

        public static IDisposable RunConnectDisconnect(bool scaleout, int nodes = 1, int connections = 1000)
        {
            IHttpClient client;
            IDisposable disposable = TryGetClient(scaleout, nodes, out client);

            for (int i = 0; i < connections; i++)
            {
                var connection = new HubConnection("http://foo");
                var proxy = connection.CreateHubProxy("SimpleEchoHub");
                var wh = new ManualResetEventSlim(false);

                proxy.On("echo", _ => wh.Set());

                try
                {
                    connection.Start(client).Wait();

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

            return disposable;
        }


        public static IDisposable TryGetClient(bool scaleout, int nodes, out IHttpClient client)
        {
            if (scaleout)
            {
                MemoryHost[] hosts;
                UseScaleout(nodes, out hosts, out client);
                return new DisposableAction(() =>
                {
                    for (int i = 0; i < nodes; i++)
                    {
                        hosts[i].Dispose();
                    }
                });
            }
            else
            {
                var host = new MemoryHost();
                host.Configure(app =>
                {
                    var config = new HubConfiguration()
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR(config);
                });

                client = host;
                return host;
            }
        }

        private static void UseScaleout(int nodes, out MemoryHost[] hosts, out IHttpClient client)
        {
            hosts = new MemoryHost[nodes];
            var eventBus = new EventBus();
            var protectedData = new DefaultProtectedData();
            for (var i = 0; i < nodes; ++i)
            {
                var host = new MemoryHost();

                host.Configure(app =>
                {
                    var config = new HubConfiguration()
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    var bus = new DelayedMessageBus(host.InstanceName, eventBus, config.Resolver, TimeSpan.Zero);
                    config.Resolver.Register(typeof(IMessageBus), () => bus);

                    app.MapSignalR(config);

                    config.Resolver.Register(typeof(IProtectedData), () => protectedData);
                });

                hosts[i] = host;
            }

            client = new LoadBalancer(hosts);
        }

        public static void Scaleout(int nodes, int clients)
        {
            var hosts = new MemoryHost[nodes];
            var random = new Random();
            var eventBus = new EventBus();
            var protectedData = new DefaultProtectedData();
            for (var i = 0; i < nodes; ++i)
            {
                var host = new MemoryHost();

                host.Configure(app =>
                {
                    var config = new HubConfiguration()
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    var delay = i % 2 == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(1);
                    var bus = new DelayedMessageBus(host.InstanceName, eventBus, config.Resolver, delay);
                    config.Resolver.Register(typeof(IMessageBus), () => bus);

                    app.MapSignalR(config);

                    config.Resolver.Register(typeof(IProtectedData), () => protectedData);
                });

                hosts[i] = host;
            }

            var client = new LoadBalancer(hosts);
            var wh = new ManualResetEventSlim();

            for (int i = 0; i < clients; i++)
            {
                Task.Run(() => RunLoop(client, wh));
            }

            wh.Wait();
        }

        private static void RunLoop(IHttpClient client, ManualResetEventSlim wh)
        {
            var connection = new HubConnection("http://foo");
            var proxy = connection.CreateHubProxy("SimpleEchoHub");
            connection.TraceLevel = Client.TraceLevels.Messages;
            var dict = new Dictionary<string, int>();

            proxy.On<int, string, string>("send", (next, connectionId, serverName) =>
            {
                if (wh.IsSet)
                {
                    return;
                }

                int value;
                if (dict.TryGetValue(connectionId, out value))
                {
                    if (value + 1 != next)
                    {
                        Console.WriteLine("{0}: Expected {1} and got {2} from {3}", connection.ConnectionId, value + 1, next, connectionId);
                        wh.Set();
                        return;
                    }
                }

                dict[connectionId] = next;

                if (connectionId == connection.ConnectionId)
                {
                    proxy.Invoke("send", next + 1).Wait();
                }
            });

            connection.Start(new Client.Transports.LongPollingTransport(client)).Wait();

            proxy.Invoke("send", 0).Wait();
        }

        public static void SendLoop()
        {
            var host = new MemoryHost();

            host.Configure(app =>
            {
                var config = new HubConfiguration()
                {
                    Resolver = new DefaultDependencyResolver()
                };

                config.Resolver.Resolve<IConfigurationManager>().ConnectionTimeout = TimeSpan.FromDays(1);
                app.MapSignalR(config);
            });


            var connection = new HubConnection("http://foo");
            var proxy = connection.CreateHubProxy("SimpleEchoHub");
            var wh = new ManualResetEventSlim(false);

            proxy.On("echo", _ => wh.Set());

            try
            {
                connection.Start(new Client.Transports.LongPollingTransport(host)).Wait();

                while (true)
                {
                    proxy.Invoke("Echo", "foo").Wait();

                    if (!wh.Wait(TimeSpan.FromSeconds(10)))
                    {
                        Debugger.Break();
                    }

                    wh.Reset();
                }
            }
            catch
            {
                connection.Stop();
            }
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

                app.MapSignalR<MyRejoinGroupConnection>("/groups", config);

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

                app.MapSignalR<RawConnection>("/Raw-connection", config);
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

        private class LoadBalancer : SignalR.Client.Http.IHttpClient
        {
            private int _counter;
            private readonly SignalR.Client.Http.IHttpClient[] _servers;
            private Random _random = new Random();

            public LoadBalancer(params SignalR.Client.Http.IHttpClient[] servers)
            {
                _servers = servers;
            }

            public void Initialize(SignalR.Client.IConnection connection)
            {
                foreach (SignalR.Client.Http.IHttpClient server in _servers)
                {
                    server.Initialize(connection);
                }
            }

            public Task<Client.Http.IResponse> Get(string url, Action<Client.Http.IRequest> prepareRequest, bool isLongRunning)
            {
                int index = _random.Next(0, _servers.Length);
                _counter = (_counter + 1) % _servers.Length;
                return _servers[index].Get(url, prepareRequest, isLongRunning);
            }

            public Task<Client.Http.IResponse> Post(string url, Action<Client.Http.IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
            {
                int index = _random.Next(0, _servers.Length);
                _counter = (_counter + 1) % _servers.Length;
                return _servers[index].Post(url, prepareRequest, postData, isLongRunning);
            }
        }

        private class EventBus
        {
            private long id;
            public event EventHandler<EventMessage> Received;

            public void Publish(int streamIndex, ScaleoutMessage message)
            {
                if (Received != null)
                {
                    lock (this)
                    {
                        Received(this, new EventMessage
                        {
                            Id = (ulong)id,
                            Message = message,
                            StreamIndex = streamIndex
                        });

                        id++;
                    }
                }
            }
        }

        private class EventMessage
        {
            public int StreamIndex { get; set; }
            public ulong Id { get; set; }
            public ScaleoutMessage Message { get; set; }
        }

        private class DelayedMessageBus : ScaleoutMessageBus
        {
            private readonly TimeSpan _delay;
            private readonly EventBus _bus;
            private readonly string _serverName;
            private TaskQueue _queue = new TaskQueue();

            public DelayedMessageBus(string serverName, EventBus bus, IDependencyResolver resolver, TimeSpan delay)
                : base(resolver, new ScaleoutConfiguration())
            {
                _serverName = serverName;
                _bus = bus;
                _delay = delay;

                _bus.Received += (sender, e) =>
                {
                    _queue.Enqueue(state =>
                    {
                        var eventMessage = (EventMessage)state;
                        return Task.Run(() => OnReceived(eventMessage.StreamIndex, eventMessage.Id, eventMessage.Message));
                    },
                    e);
                };

                Open(0);
            }

            protected override Task Send(IList<Message> messages)
            {
                _bus.Publish(0, new ScaleoutMessage(messages));

                return TaskAsyncHelper.Empty;
            }

            protected override void OnReceived(int streamIndex, ulong id, ScaleoutMessage message)
            {
                string value = message.Messages[0].GetString();

                if (!value.Contains("ServerCommandType"))
                {
                    if (_delay != TimeSpan.Zero)
                    {
                        Thread.Sleep(_delay);
                    }

                    Console.WriteLine("{0}: OnReceived({1}, {2}, {3})", _serverName, streamIndex, id, value);
                }

                base.OnReceived(streamIndex, id, message);
            }
        }
    }
}
