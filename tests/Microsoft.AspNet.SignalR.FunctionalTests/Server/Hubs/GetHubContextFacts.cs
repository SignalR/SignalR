using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class GetHubContextFacts
    {
        [Fact]
        public async Task SendToGroupFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext<SendToSome, IBasicClient>();
                });

                var connection1 = new HubConnection("http://foo/");

                using (connection1)
                {
                    var wh1 = new AsyncManualResetEvent(initialState: false);

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", wh1.Set);

                    hubContext.Groups.Add(connection1.ConnectionId, "Foo").Wait();
                    hubContext.Clients.Group("Foo").send();

                    Assert.True(await wh1.WaitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Fact]
        public async Task SendToUserFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = HubFacts.InitializeUserByQuerystring(host);

                var wh = new AsyncManualResetEvent();

                using (var connection = HubFacts.GetUserConnection("myuser"))
                {
                    var hub = connection.CreateHubProxy("SendToSome");

                    await connection.Start(host);

                    hub.On("send", wh.Set);

                    hubContext.Clients.User("myuser").send();

                    Assert.True(await wh.WaitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Fact]
        public async Task SendToUsersFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = HubFacts.InitializeUserByQuerystring(host);

                var wh1 = new AsyncManualResetEvent();
                var wh2 = new AsyncManualResetEvent();

                var connection1 = HubFacts.GetUserConnection("myuser");
                var connection2 = HubFacts.GetUserConnection("myuser2");

                using (connection1)
                using (connection2)
                {
                    var proxy1 = connection1.CreateHubProxy("SendToSome");
                    var proxy2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host);
                    await connection2.Start(host);

                    proxy1.On("send", wh1.Set);
                    proxy2.On("send", wh2.Set);

                    hubContext.Clients.Users(new List<string> { "myuser", "myuser2" }).send();

                    Assert.True(await wh1.WaitAsync(TimeSpan.FromSeconds(10)));
                    Assert.True(await wh2.WaitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Fact]
        public async Task SendToGroupsFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext<SendToSome, IBasicClient>();
                });

                var connection1 = new HubConnection("http://foo/");

                using (connection1)
                {
                    var wh1 = new AsyncManualResetEvent(initialState: false);

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", () => wh1.Set());

                    await hubContext.Groups.Add(connection1.ConnectionId, "Foo");
                    hubContext.Clients.Groups(new[] { "Foo" }).send();

                    Assert.True(await wh1.WaitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Fact]
        public async Task SendToSpecificClientFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext<SendToSome, IBasicClient>();
                });

                var connection1 = new HubConnection("http://foo/");

                using (connection1)
                {
                    var wh1 = new AsyncManualResetEvent(initialState: false);

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", wh1.Set);

                    hubContext.Clients.Client(connection1.ConnectionId).send();

                    Assert.True(await wh1.WaitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Fact]
        public async Task SendToSpecificClientsFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext<SendToSome, IBasicClient>();
                });

                var connection1 = new HubConnection("http://foo/");

                using (connection1)
                {
                    var wh1 = new AsyncManualResetEvent(initialState: false);

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", wh1.Set);

                    hubContext.Clients.Clients(new[] { connection1.ConnectionId }).send();

                    Assert.True(await wh1.WaitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Fact]
        public async Task SendToAllFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext<SendToSome, IBasicClient>();
                });

                var connection1 = new HubConnection("http://foo/");
                var connection2 = new HubConnection("http://foo/");

                using (connection1)
                using (connection2)
                {
                    var wh1 = new AsyncManualResetEvent(initialState: false);
                    var wh2 = new AsyncManualResetEvent(initialState: false);

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host);
                    await connection2.Start(host);

                    hub1.On("send", wh1.Set);
                    hub2.On("send", wh2.Set);

                    hubContext.Clients.All.send();

                    Assert.True(await wh1.WaitAsync(TimeSpan.FromSeconds(10)));
                    Assert.True(await wh2.WaitAsync(TimeSpan.FromSeconds(10)));
                }
            }
        }
    }
}
