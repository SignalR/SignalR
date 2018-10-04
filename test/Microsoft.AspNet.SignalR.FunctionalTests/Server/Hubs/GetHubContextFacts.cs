// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                    var wh1 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", () => wh1.TrySetResult(null));

                    await hubContext.Groups.Add(connection1.ConnectionId, "Foo");
                    hubContext.Clients.Group("Foo").send();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Fact]
        public async Task SendToUserFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = HubFacts.InitializeUserByQuerystring(host);

                var wh = new TaskCompletionSource<object>();

                using (var connection = HubFacts.GetUserConnection("myuser"))
                {
                    var hub = connection.CreateHubProxy("SendToSome");

                    await connection.Start(host);

                    hub.On("send", () => wh.TrySetResult(null));

                    hubContext.Clients.User("myuser").send();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Fact]
        public async Task SendToUsersFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                IHubContext<IBasicClient> hubContext = HubFacts.InitializeUserByQuerystring(host);

                var wh1 = new TaskCompletionSource<object>();
                var wh2 = new TaskCompletionSource<object>();

                var connection1 = HubFacts.GetUserConnection("myuser");
                var connection2 = HubFacts.GetUserConnection("myuser2");

                using (connection1)
                using (connection2)
                {
                    var proxy1 = connection1.CreateHubProxy("SendToSome");
                    var proxy2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host);
                    await connection2.Start(host);

                    proxy1.On("send", () => wh1.TrySetResult(null));
                    proxy2.On("send", () => wh2.TrySetResult(null));

                    hubContext.Clients.Users(new List<string> { "myuser", "myuser2" }).send();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
                    await wh2.Task.OrTimeout(TimeSpan.FromSeconds(10));
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
                    var wh1 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", () => wh1.TrySetResult(null));

                    await hubContext.Groups.Add(connection1.ConnectionId, "Foo");
                    hubContext.Clients.Groups(new[] { "Foo" }).send();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
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
                    var wh1 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", () => wh1.TrySetResult(null));

                    hubContext.Clients.Client(connection1.ConnectionId).send();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
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
                    var wh1 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");

                    await connection1.Start(host);

                    hub1.On("send", () => wh1.TrySetResult(null));

                    hubContext.Clients.Clients(new[] { connection1.ConnectionId }).send();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
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
                    var wh1 = new TaskCompletionSource<object>();
                    var wh2 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host);
                    await connection2.Start(host);

                    hub1.On("send", () => wh1.TrySetResult(null));
                    hub2.On("send", () => wh2.TrySetResult(null));

                    hubContext.Clients.All.send();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
                    await wh2.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }
    }
}
