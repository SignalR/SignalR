// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Moq;
using Newtonsoft.Json.Linq;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task ReadingState(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    hub["name"] = "test";

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<string>("ReadStateValue").OrTimeout();

                    Assert.Equal("test", result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task ReadingStateFromVB(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            // Force the VB Sample assembly to be loaded into the current AppDomain for MemoryHost
            var foo = new Microsoft.AspNet.SignalR.Samples.VB.VBDemoHub();

            using (var host = CreateHost(hostType, transportType))
            {
                var mre = new TaskCompletionSource<object>();

                host.Initialize(messageBusType: messageBusType);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("VBDemo");

                    hub.On("anyMethodNameWillDo", () => mre.TrySetResult(null));

                    await connection.Start(host.Transport);

                    await mre.Task.OrTimeout(TimeSpan.FromSeconds(2));

                    var originalMessage = await hub.Invoke<string>("ReadStateValue").OrTimeout();

                    hub["message"] = "test VB.NET";

                    var newMessage = await hub.Invoke<string>("ReadStateValue").OrTimeout();

                    Assert.Equal("Why?", originalMessage);
                    Assert.Equal("test VB.NET", newMessage);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task ReadingComplexState(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    hub["state"] = JToken.FromObject(new
                    {
                        Name = "David",
                        Address = new
                        {
                            Street = "St"
                        }
                    });

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<dynamic>("ReadAnyState").OrTimeout();
                    dynamic state2 = hub["state2"];
                    dynamic addy = hub["addy"];

                    Assert.NotNull(result);
                    Assert.NotNull(state2);
                    Assert.NotNull(addy);
                    Assert.Equal("David", (string)result.Name);
                    Assert.Equal("St", (string)result.Address.Street);
                    Assert.Equal("David", (string)state2.Name);
                    Assert.Equal("St", (string)state2.Address.Street);
                    Assert.Equal("St", (string)addy.Street);
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling)]
        public async Task BasicAuthCredentialsFlow(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = CreateHubConnection(host, path: "/basicauth/signalr", useDefaultUrl: false);
                var proxy = connection.CreateHubProxy("AuthenticatedEchoHub");

                var tcs = new TaskCompletionSource<string>();

                using (connection)
                {
                    connection.Credentials = new System.Net.NetworkCredential("user", "password");

                    proxy.On<string>("echo", data =>
                    {
                        tcs.TrySetResult(data);
                    });

                    await connection.Start(host.Transport);

                    await proxy.Invoke("EchoCallback", "Hello World").OrTimeout();

                    Assert.Equal("Hello World", await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData("SendToSome")]
        [InlineData("SendToSomeTyped")]
        public Task CanSendToMultipleUsers(string hubName)
        {
            return SendToMultipleUsers(hubName);
        }

        private async Task SendToMultipleUsers(string hubName)
        {
            using (var host = new MemoryHost())
            {
                InitializeUserByQuerystring(host);

                var wh1 = new TaskCompletionSource<object>();
                var wh2 = new TaskCompletionSource<object>();

                var connection1 = GetUserConnection("myUser");
                var connection2 = GetUserConnection("myUser2");

                using (connection1)
                using (connection2)
                {
                    var proxy1 = connection1.CreateHubProxy(hubName);
                    var proxy2 = connection2.CreateHubProxy(hubName);

                    proxy1.On("send", () => wh1.TrySetResult(null));
                    proxy1.On("send", () => wh2.TrySetResult(null));

                    await connection1.Start(host);
                    await connection2.Start(host);

                    await proxy1.Invoke("SendToUsers", new List<string> { "myUser", "myUser2" });

                    await wh1.Task.OrTimeout();
                    await wh2.Task.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task CanSendViaUser()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var resolver = new DefaultDependencyResolver();
                    var config = new HubConfiguration
                    {
                        Resolver = resolver
                    };
                    var provider = new Mock<IUserIdProvider>();
                    provider.Setup(m => m.GetUserId(It.IsAny<IRequest>()))
                            .Returns<IRequest>(request =>
                            {
                                return request.QueryString["name"];
                            });

                    config.Resolver.Register(typeof(IUserIdProvider), () => provider.Object);
                    app.MapSignalR(config);
                });

                var qs = new Dictionary<string, string>
                {
                    { "name", "myuser" }
                };

                var wh = new TaskCompletionSource<object>();

                using (var connection = new HubConnection("http://memoryhost", qs))
                {
                    var proxy = connection.CreateHubProxy("demo");

                    proxy.On("invoke", () =>
                    {
                        wh.TrySetResult(null);
                    });

                    await connection.Start(host);

                    await proxy.Invoke("SendToUser", "myuser");

                    await wh.Task.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task CanSendViaUserWhenPrincipalSet()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };
                    app.Use((context, next) =>
                    {
                        var identity = new GenericIdentity("randomUserId");
                        context.Request.User = new GenericPrincipal(identity, new string[0]);
                        return next();
                    });

                    app.MapSignalR(config);
                });

                var wh = new TaskCompletionSource<object>();

                using (var connection = new HubConnection("http://memoryhost"))
                {
                    var proxy = connection.CreateHubProxy("demo");

                    proxy.On("invoke", () =>
                    {
                        wh.TrySetResult(null);
                    });

                    await connection.Start(host);

                    await proxy.Invoke("SendToUser", "randomUserId");

                    await wh.Task.OrTimeout();
                }
            }
        }

        //[Fact(Skip = "Disable IIS Express tests because they fail to initialize")]
        public async Task SendToUser()
        {
            using (var host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();

                var connection1 = CreateAuthHubConnection(host, "user1", "password");
                var connection2 = CreateAuthHubConnection(host, "user1", "password");
                var connection3 = CreateAuthHubConnection(host, "user1", "password");
                var connection4 = CreateAuthHubConnection(host, "user2", "password");

                var wh1 = new TaskCompletionSource<object>();
                var wh2 = new TaskCompletionSource<object>();
                var wh3 = new TaskCompletionSource<object>();
                var wh4 = new TaskCompletionSource<object>();

                var hub1 = connection1.CreateHubProxy("AuthenticatedEchoHub");
                var hub2 = connection2.CreateHubProxy("AuthenticatedEchoHub");
                var hub3 = connection3.CreateHubProxy("AuthenticatedEchoHub");
                var hub4 = connection4.CreateHubProxy("AuthenticatedEchoHub");
                hub1.On("echo", () => wh1.TrySetResult(null));
                hub2.On("echo", () => wh2.TrySetResult(null));
                hub3.On("echo", () => wh3.TrySetResult(null));
                hub4.On("echo", () => wh4.TrySetResult(null));

                using (connection1)
                {
                    using (connection2)
                    {
                        using (connection3)
                        {
                            using (connection4)
                            {
                                await connection1.Start(new Microsoft.AspNet.SignalR.Client.Transports.WebSocketTransport());
                                await connection2.Start(new Microsoft.AspNet.SignalR.Client.Transports.ServerSentEventsTransport());
                                await connection3.Start(new Microsoft.AspNet.SignalR.Client.Transports.LongPollingTransport());
                                await connection4.Start();

                                await hub4.Invoke("SendToUser", "user1", "message");

                                await wh1.Task.OrTimeout();
                                await wh2.Task.OrTimeout();
                                await wh3.Task.OrTimeout();
                                Assert.False(wh4.Task.IsCompleted);
                            }
                        }
                    }
                }
            }
        }

        //[Fact(Skip = "Disable IIS Express tests because they fail to initialize")]
        public async Task SendToUserConnectDisconnectEvents()
        {
            using (var host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();

                var connection1 = CreateAuthHubConnection(host, "user1", "password");
                var connection2 = CreateAuthHubConnection(host, "user2", "password");

                var connected = 0;
                var disconnected = 0;
                var wh1 = new TaskCompletionSource<object>();
                var wh2 = new TaskCompletionSource<object>();

                var hub1 = connection1.CreateHubProxy("AuthenticatedEchoHub");
                var hub2 = connection2.CreateHubProxy("AuthenticatedEchoHub");
                hub1.On<string>("SendUserOnConnected", (user) =>
                {
                    if (++connected >= 2)
                    {
                        wh1.TrySetResult(null);
                    }
                });
                hub1.On<string>("SendUserOnDisconnected", (user) =>
                {
                    if (++disconnected >= 1)
                    {
                        wh2.TrySetResult(null);
                    }
                });

                using (connection1)
                {
                    using (connection2)
                    {
                        await connection1.Start();
                        await connection2.Start(new Microsoft.AspNet.SignalR.Client.Transports.WebSocketTransport());

                        await wh1.Task.OrTimeout();
                    }

                    await wh2.Task.OrTimeout();
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task VerifyOwinContext(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("MyItemsHub");
                var hub1 = connection2.CreateHubProxy("MyItemsHub");

                var results = new List<RequestItemsResponse>();
                hub1.On<RequestItemsResponse>("update", result =>
                {
                    if (!results.Contains(result))
                    {
                        results.Add(result);
                    }
                });

                using (connection2)
                {
                    using (connection)
                    {
                        await connection.Start(host.TransportFactory());
                        await connection2.Start(host.TransportFactory());

                        await Task.Delay(TimeSpan.FromSeconds(2));

                        await hub1.Invoke("GetItems").OrTimeout();

                        await Task.Delay(TimeSpan.FromSeconds(2));

                        connection.Stop();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));

                    Debug.WriteLine(String.Join(", ", results));

                    Assert.Equal(3, results.Count);
                    Assert.Equal("OnConnected", results[0].Method);
                    Assert.NotNull(results[0].Headers);
                    Assert.NotNull(results[0].Query);
                    Assert.True(results[0].Headers.Count > 0);
                    Assert.True(results[0].Query.Count > 0);
                    Assert.True(results[0].OwinKeys.Length > 0);
                    Assert.Equal("nosniff", results[0].XContentTypeOptions);
                    Assert.Equal("GetItems", results[1].Method);
                    Assert.NotNull(results[1].Headers);
                    Assert.NotNull(results[1].Query);
                    Assert.True(results[1].Headers.Count > 0);
                    Assert.True(results[1].Query.Count > 0);
                    Assert.True(results[1].OwinKeys.Length > 0);
                    Assert.Equal("OnDisconnected", results[2].Method);
                    Assert.NotNull(results[2].Headers);
                    Assert.NotNull(results[2].Query);
                    Assert.True(results[2].Headers.Count > 0);
                    Assert.True(results[2].Query.Count > 0);
                    Assert.True(results[2].OwinKeys.Length > 0);

                    connection2.Stop();
                }
            }
        }

        //[Fact(Skip = "Disable IIS Express tests because they fail to initialize")]
        public async Task HttpHandlersAreNotSetInIISIntegratedPipeline()
        {
            using (var host = CreateHost(HostType.IISExpress, TransportType.LongPolling))
            {
                host.Initialize();
                var connection = CreateHubConnection(host, "/session");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");
                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<string>("GetHttpContextHandler");

                    Assert.Null(result);
                }
            }
        }

        [Theory]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SettingState(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");
                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<string>("SetStateValue", "test").OrTimeout();

                    Assert.Equal("test", result);
                    Assert.Equal("test", hub["Company"]);
                }
            }
        }

        [Theory]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task CancelledTask(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");
                    await connection.Start(host.Transport);

                    await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("CancelledTask")).OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task CancelledGenericTask(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");
                    await connection.Start(host.Transport);

                    await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("CancelledGenericTask")).OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task GetValueFromServer(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<int>("GetValue").OrTimeout();

                    Assert.Equal(10, result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SynchronousException(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("SynchronousException").OrTimeout());

                    Assert.Contains("System.Exception", ex.Message);
                }
            }
        }


        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task TaskWithException(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("TaskWithException").OrTimeout());

                    Assert.Contains("System.Exception", ex.Message);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task GenericTaskWithException(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("GenericTaskWithException").OrTimeout());

                    Assert.Contains("System.Exception", ex.Message);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task DetailedErrorsAreDisabledByDefault(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateHubConnection(host, "/signalr2/test", useDefaultUrl: false))
                {
                    connection.TraceWriter = host.ClientTraceOutput;

                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.TransportFactory());

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("TaskWithException").OrTimeout());

                    Assert.DoesNotContain("System.Exception", ex.GetBaseException().Message);
                    Assert.Contains("demo.TaskWithException", ex.GetBaseException().Message);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task DetailedErrorsAreAlwaysGivenForHubExceptions(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateHubConnection(host, "/signalr2/test", useDefaultUrl: false))
                {
                    connection.TraceWriter = host.ClientTraceOutput;

                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.TransportFactory());

                    var hubEx = await Assert.ThrowsAsync<Client.HubException>(() => hub.Invoke("HubException").OrTimeout());

                    Assert.Equal("message", hubEx.Message);
                    Assert.Equal("errorData", (string)hubEx.ErrorData);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task DetailedErrorsAreAlwaysGivenForHubExceptionsWithoutErrorData(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateHubConnection(host, "/signalr2/test", useDefaultUrl: false))
                {
                    connection.TraceWriter = host.ClientTraceOutput;

                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.TransportFactory());

                    var hubEx = await Assert.ThrowsAsync<Client.HubException>(() => hub.Invoke("HubExceptionWithoutErrorData").OrTimeout());

                    Assert.Equal("message", hubEx.Message);
                    Assert.Null(hubEx.ErrorData);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task GenericTaskWithContinueWith(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<int>("GenericTaskWithContinueWith").OrTimeout();

                    Assert.Equal(4, result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task Overloads(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    await hub.Invoke("Overload").OrTimeout();
                    var n = await hub.Invoke<int>("Overload", 1).OrTimeout();

                    Assert.Equal(1, n);
                }
            }
        }

        [Theory]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task ReturnDataWithPlus(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("echoHub");

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<string>("EchoReturn", "+").OrTimeout();

                    Assert.Equal("+", result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task CallbackDataWithPlus(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("echoHub");
                    var tcs = new TaskCompletionSource<string>();
                    hub.On<string>("echo", (data) =>
                    {
                        tcs.TrySetResult(data);
                    });

                    await connection.Start(host.Transport);

                    await hub.Invoke("EchoCallback", "+").OrTimeout();

                    Assert.Equal("+", await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task UnsupportedOverloads(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var expectedErrorMessage = "'UnsupportedOverload' method could not be resolved. Potential candidates are: \n" +
                                               "UnsupportedOverload(x:String):Void\n" +
                                               "UnsupportedOverload(x:Int32):Void";

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("UnsupportedOverload", 13177).OrTimeout());
                    Assert.Equal(ex.Message, expectedErrorMessage);
                }
            }
        }

        [Fact]
        public async Task ChangeHubUrl()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration()
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR("/foo", config);
                });

                var connection = new HubConnection("http://site/foo", useDefaultUrl: false, queryString: new Dictionary<string, string> { { "test", "ChangeHubUrl" } });

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    var wh = new TaskCompletionSource<object>();

                    hub.On("signal", id =>
                    {
                        Assert.NotNull(id);
                        wh.TrySetResult(null);
                    });

                    await connection.Start(host);

                    await hub.Invoke("DynamicTask").OrTimeout();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ChangeHubUrlAspNet(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var query = new Dictionary<string, string> { { "test", GetTestName() } };
                SetHostData(host, query);

                var connection = new HubConnection(host.Url + "/signalr2/test", useDefaultUrl: false, queryString: query);

                using (connection)
                {
                    connection.TraceWriter = host.ClientTraceOutput;

                    var hub = connection.CreateHubProxy("demo");

                    var wh = new TaskCompletionSource<object>();

                    hub.On("signal", id =>
                    {
                        Assert.NotNull(id);
                        wh.TrySetResult(null);
                    });

                    await connection.Start(host.Transport);

                    await hub.Invoke("DynamicTask").OrTimeout();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task GuidTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    var wh = new TaskCompletionSource<object>();

                    hub.On<Guid>("TestGuid", id =>
                    {
                        Assert.NotNull(id);
                        wh.TrySetResult(null);
                    });

                    await connection.Start(host.Transport);

                    await hub.Invoke("TestGuid").OrTimeout();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task RemainsConnectedWithHubsAppendedToUrl(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = CreateHubConnection(host, "/signalr/js", useDefaultUrl: false);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    var tcs = new TaskCompletionSource<object>();
                    var testGuidInvocations = 0;

                    hub.On<Guid>("TestGuid", id =>
                    {
                        testGuidInvocations++;
                        if (testGuidInvocations < 2)
                        {
                            hub.Invoke("TestGuid").ContinueWithPreservedCulture(t =>
                                {
                                    if (t.IsFaulted)
                                    {
                                        tcs.SetUnwrappedException(t.Exception);
                                    }
                                    else if (t.IsCanceled)
                                    {
                                        tcs.TrySetCanceled();
                                    }
                                },
                                TaskContinuationOptions.NotOnRanToCompletion);
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
                    });

                    connection.Error += e => tcs.SetException(e);
                    connection.Reconnecting += () => tcs.TrySetCanceled();

                    await connection.Start(host.Transport);

                    await hub.Invoke("TestGuid").OrTimeout();

                    await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Fact]
        public void HubHasConnectionEvents()
        {
            var type = typeof(Hub);

            // Hub has the disconnect method
            Assert.True(type.GetMethod("OnDisconnected", new Type[] { typeof(bool) }) != null);

            // Hub has the connect method
            Assert.True(type.GetMethod("OnConnected") != null);

            // Hub has the reconnect method
            Assert.True(type.GetMethod("OnReconnected") != null);
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task ComplexPersonState(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    var person = new SignalR.Samples.Hubs.DemoHub.DemoHub.Person
                    {
                        Address = new SignalR.Samples.Hubs.DemoHub.DemoHub.Address
                        {
                            Street = "Redmond",
                            Zip = "98052"
                        },
                        Age = 25,
                        Name = "David"
                    };

                    var person1 = await hub.Invoke<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>("ComplexType", person).OrTimeout();
                    var person2 = hub.GetValue<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>("person");

                    Assert.NotNull(person1);
                    Assert.NotNull(person2);
                    Assert.Equal("David", person1.Name);
                    Assert.Equal("David", person2.Name);
                    Assert.Equal(25, person1.Age);
                    Assert.Equal(25, person2.Age);
                    Assert.Equal("Redmond", person1.Address.Street);
                    Assert.Equal("Redmond", person2.Address.Street);
                    Assert.Equal("98052", person1.Address.Zip);
                    Assert.Equal("98052", person2.Address.Zip);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task DynamicInvokeTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var callback = @"!!!|\CallMeBack,,,!!!";

                    var hub = connection.CreateHubProxy("demo");

                    var wh = new TaskCompletionSource<object>();

                    hub.On(callback, () => wh.TrySetResult(null));

                    await connection.Start(host.Transport);

                    await hub.Invoke("DynamicInvoke", callback).OrTimeout();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task CreateProxyAfterConnectionStartsThrows(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var connection = CreateHubConnection(host);

                try
                {
                    await connection.Start(host.Transport);
                    Assert.Throws<InvalidOperationException>(() => connection.CreateHubProxy("demo"));
                }
                finally
                {
                    connection.Stop();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task AddingToMultipleGroups(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var max = 10;

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var proxy = connection.CreateHubProxy("MultGroupHub");

                    proxy.On<User>("onRoomJoin", user =>
                    {
                        Assert.True(countDown.Mark(user.Index));
                    });

                    await connection.Start(host.Transport);

                    for (var i = 0; i < max; i++)
                    {
                        var user = new User { Index = i, Name = "tester", Room = "test" + i };
                        await proxy.Invoke("login", user).OrTimeout();
                        await proxy.Invoke("joinRoom", user).OrTimeout();
                    }

                    try
                    {
                        await countDown.WaitAsync().OrTimeout(TimeSpan.FromSeconds(30));
                    }
                    catch (TimeoutException)
                    {
                        Assert.True(false, "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
                    }
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task HubGroupsRejoinWhenAutoRejoiningGroupsEnabled(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: null,
                                disconnectTimeout: 6,
                                connectionTimeout: 2,
                                enableAutoRejoiningGroups: true,
                                messageBusType: messageBusType);

                var max = 10;

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                var countDownAfterReconnect = new CountDownRange<int>(Enumerable.Range(max, max));
                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var proxy = connection.CreateHubProxy("MultGroupHub");

                    proxy.On<User>("onRoomJoin", u =>
                    {
                        if (u.Index < max)
                        {
                            Assert.True(countDown.Mark(u.Index));
                        }
                        else
                        {
                            Assert.True(countDownAfterReconnect.Mark(u.Index));
                        }
                    });

                    await connection.Start(host.Transport);

                    var user = new User { Name = "tester" };
                    await proxy.Invoke("login", user).OrTimeout();

                    for (var i = 0; i < max; i++)
                    {
                        user.Index = i;
                        await proxy.Invoke("joinRoom", user).OrTimeout();
                    }

                    // Force Reconnect
                    await Task.Delay(TimeSpan.FromSeconds(3));

                    for (var i = max; i < 2 * max; i++)
                    {
                        user.Index = i;
                        await proxy.Invoke("joinRoom", user).OrTimeout();
                    }

                    try
                    {
                        await countDown.WaitAsync().OrTimeout(TimeSpan.FromSeconds(30));
                    }
                    catch (TimeoutException)
                    {
                        Assert.True(false, "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
                    }
                    try
                    {
                        await countDownAfterReconnect.WaitAsync().OrTimeout(TimeSpan.FromSeconds(30));
                    }
                    catch (TimeoutException)
                    {
                        Assert.True(false, "Didn't receive " + max + " messages. Got " + (max - countDownAfterReconnect.Count) + " missed " + String.Join(",", countDownAfterReconnect.Left.Select(i => i.ToString())));
                    }
                }
            }
        }

        [Fact]
        public async Task RejoiningGroupsOnlyReceivesGroupsBelongingToHub()
        {
            var logRejoiningGroups = new LogRejoiningGroupsModule();
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR("/signalr", config);

                    config.Resolver.Resolve<IHubPipeline>().AddModule(logRejoiningGroups);
                    var configuration = config.Resolver.Resolve<IConfigurationManager>();
                    // The following sets the heartbeat to 1 s
                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
                    configuration.KeepAlive = null;
                    configuration.ConnectionTimeout = TimeSpan.FromSeconds(2);
                });

                var connection = new HubConnection("http://foo");

                using (connection)
                {
                    var proxy = connection.CreateHubProxy("MultGroupHub");
                    var proxy2 = connection.CreateHubProxy("MultGroupHub2");

                    await connection.Start(host).OrTimeout();

                    var user = new User { Name = "tester" };
                    await proxy.Invoke("login", user).OrTimeout();
                    await proxy2.Invoke("login", user).OrTimeout();

                    // Force Reconnect
                    await Task.Delay(TimeSpan.FromSeconds(3));

                    await proxy.Invoke("joinRoom", user).OrTimeout();
                    await proxy2.Invoke("joinRoom", user).OrTimeout();

                    await Task.Delay(TimeSpan.FromSeconds(3));

                    Assert.Contains("foo", logRejoiningGroups.GroupsRejoined["MultGroupHub"]);
                    Assert.Contains("tester", logRejoiningGroups.GroupsRejoined["MultGroupHub"]);
                    Assert.DoesNotContain("foo2", logRejoiningGroups.GroupsRejoined["MultGroupHub"]);
                    Assert.DoesNotContain("tester2", logRejoiningGroups.GroupsRejoined["MultGroupHub"]);
                    Assert.Contains("foo2", logRejoiningGroups.GroupsRejoined["MultGroupHub2"]);
                    Assert.Contains("tester2", logRejoiningGroups.GroupsRejoined["MultGroupHub2"]);
                    Assert.DoesNotContain("foo", logRejoiningGroups.GroupsRejoined["MultGroupHub2"]);
                    Assert.DoesNotContain("tester", logRejoiningGroups.GroupsRejoined["MultGroupHub2"]);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task CustomQueryStringRaw(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection = new HubConnection(host.Url, "a=b&test=" + GetTestName());

                using (connection)
                {
                    connection.TraceWriter = host.ClientTraceOutput;

                    var hub = connection.CreateHubProxy("CustomQueryHub");

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<string>("GetQueryString", "a").OrTimeout();

                    Assert.Equal("b", result);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task CustomQueryString(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);
                var qs = new Dictionary<string, string>();
                qs["a"] = "b";
                qs["test"] = GetTestName();
                var connection = new HubConnection(host.Url, qs);

                using (connection)
                {
                    connection.TraceWriter = host.ClientTraceOutput;

                    var hub = connection.CreateHubProxy("CustomQueryHub");

                    await connection.Start(host.Transport);

                    var result = await hub.Invoke<string>("GetQueryString", "a").OrTimeout();

                    Assert.Equal("b", result);
                }
            }
        }

        [Fact]
        public async Task ReturningNullFromConnectAndDisconnectAccepted()
        {
            var mockHub = new Mock<SomeHub>() { CallBase = true };
            mockHub.Setup(h => h.OnConnected()).Returns<Task>(null).Verifiable();
            mockHub.Setup(h => h.OnDisconnected(true)).Returns<Task>(null).Verifiable();

            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR("/signalr", config);

                    var configuration = config.Resolver.Resolve<IConfigurationManager>();
                    // The below effectively sets the heartbeat interval to one second.
                    configuration.KeepAlive = TimeSpan.FromSeconds(2);
                    config.Resolver.Register(typeof(SomeHub), () => mockHub.Object);
                });

                var connection = new HubConnection("http://foo");

                var hub = connection.CreateHubProxy("SomeHub");
                await connection.Start(host).OrTimeout();

                connection.Stop();
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            mockHub.Verify();
        }

        [Fact]
        public async Task ReturningNullFromReconnectAccepted()
        {
            var mockHub = new Mock<SomeHub>() { CallBase = true };
            mockHub.Setup(h => h.OnReconnected()).Returns<Task>(null).Verifiable();

            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR("/signalr", config);

                    var configuration = config.Resolver.Resolve<IConfigurationManager>();
                    // The following sets the heartbeat to 1 s
                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
                    configuration.KeepAlive = null;
                    configuration.ConnectionTimeout = TimeSpan.FromSeconds(2);
                    config.Resolver.Register(typeof(SomeHub), () => mockHub.Object);
                });

                var connection = new HubConnection("http://foo");

                var hub = connection.CreateHubProxy("SomeHub");
                await connection.Start(host).OrTimeout();

                // Force Reconnect
                await Task.Delay(TimeSpan.FromSeconds(3));

                await hub.Invoke("AllFoo").OrTimeout();

                await Task.Delay(TimeSpan.FromSeconds(3));

                connection.Stop();

                mockHub.Verify();
            }
        }

        [Fact]
        public void UsingHubAfterManualCreationThrows()
        {
            var hub = new SomeHub();
            Assert.Throws<InvalidOperationException>(() => hub.AllFoo());
            Assert.Throws<InvalidOperationException>(() => hub.OneFoo());
        }

        [Fact]
        public async Task CreatedHubsGetDisposed()
        {
            var mockHubs = new List<Mock<IHub>>();

            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR("/signalr", config);

                    config.Resolver.Register(typeof(IHub), () =>
                    {
                        var mockHub = new Mock<IHub>() { CallBase = true };

                        mockHubs.Add(mockHub);
                        return mockHub.Object;
                    });
                });

                var connection = new HubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host);

                    var result = await hub.Invoke<string>("ReadStateValue").OrTimeout();

                    foreach (var mockDemoHub in mockHubs)
                    {
                        mockDemoHub.Verify(d => d.Dispose(), Times.Once());
                    }
                }
            }
        }

        [Theory]
        [InlineData(MessageBusType.Default)]
        [InlineData(MessageBusType.Fake)]
        [InlineData(MessageBusType.FakeMultiStream)]
        public async Task JoiningGroupMultipleTimesGetsMessageOnce(MessageBusType messagebusType)
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    UseMessageBus(messagebusType, config.Resolver);

                    app.MapSignalR(config);
                });

                var connection = new HubConnection("http://foo");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("SendToSome");
                    var invocations = 0;

                    await connection.Start(host);

                    hub.On("send", () =>
                    {
                        invocations++;
                    });

                    // Join the group multiple times
                    await hub.Invoke("JoinGroup", "a").OrTimeout();
                    await hub.Invoke("JoinGroup", "a").OrTimeout();
                    await hub.Invoke("JoinGroup", "a").OrTimeout();
                    await hub.Invoke("SendToGroup", "a").OrTimeout();

                    await Task.Delay(TimeSpan.FromSeconds(3));

                    Assert.Equal(1, invocations);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SendToAllButCaller(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                using (connection1)
                using (connection2)
                {
                    var wh1 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var wh2 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host.TransportFactory());
                    await connection2.Start(host.TransportFactory());

                    hub1.On("send", () => wh1.TrySetResult(null));
                    hub2.On("send", () => wh2.TrySetResult(null));

                    await hub1.Invoke("SendToAllButCaller").OrTimeout();

                    await wh2.Task.OrTimeout(TimeSpan.FromSeconds(10));
                    Assert.False(wh1.Task.IsCompleted);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SendToAllButCallerInGroup(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                using (connection1)
                using (connection2)
                {
                    var tcs1 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var tcs2 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host.TransportFactory());
                    await connection2.Start(host.TransportFactory());

                    hub1.On("send", () => tcs1.TrySetResult(null));
                    hub2.On("send", () => tcs2.TrySetResult(null));

                    await hub1.Invoke("JoinGroup", "group").OrTimeout();
                    await hub2.Invoke("JoinGroup", "group").OrTimeout();

                    await hub1.Invoke("AllInGroupButCaller", "group").OrTimeout();

                    await tcs2.Task.OrTimeout(TimeSpan.FromSeconds(10));
                    Assert.False(tcs1.Task.IsCompleted);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SendToAll(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                using (connection1)
                using (connection2)
                {
                    var wh1 = new TaskCompletionSource<object>();
                    var wh2 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host.TransportFactory());
                    await connection2.Start(host.TransportFactory());

                    hub1.On("send", () => wh1.TrySetResult(null));
                    hub2.On("send", () => wh2.TrySetResult(null));

                    await hub1.Invoke("SendToAll").OrTimeout();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
                    await wh2.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SendToSelf(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                using (connection1)
                using (connection2)
                {
                    var wh1 = new TaskCompletionSource<object>();
                    var wh2 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host.TransportFactory());
                    await connection2.Start(host.TransportFactory());

                    hub1.On("send", () => wh1.TrySetResult(null));
                    hub2.On("send", () => wh2.TrySetResult(null));

                    await hub1.Invoke("SendToSelf").OrTimeout();

                    await wh1.Task.OrTimeout(TimeSpan.FromSeconds(10));
                    Assert.False(wh2.Task.IsCompleted);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SendToSpecificConnections(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                using (connection1)
                using (connection2)
                {
                    var wh1 = new TaskCompletionSource<object>();
                    var wh2 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host.TransportFactory());
                    await connection2.Start(host.TransportFactory());

                    hub1.On("send", () => wh1.TrySetResult(null));
                    hub2.On("send", () => wh2.TrySetResult(null));

                    await hub1.Invoke("SendToConnections", new List<string> { connection1.ConnectionId, connection2.ConnectionId }).OrTimeout();

                    await wh1.Task.OrTimeout();
                    await wh2.Task.OrTimeout();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task SendToEmptyConnectionsList(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("SendToSome");

                    await connection.Start(host.TransportFactory());

                    await hub.Invoke("SendToConnections", new List<string> { }).OrTimeout();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task SendToEmptyGroupsList(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("SendToSome");

                    await connection.Start(host.TransportFactory());

                    await hub.Invoke("SendToGroups", new List<string> { }).OrTimeout();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SendToSpecificGroups(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                using (connection1)
                using (connection2)
                {
                    var wh1 = new TaskCompletionSource<object>();
                    var wh2 = new TaskCompletionSource<object>();

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host.TransportFactory());
                    await connection2.Start(host.TransportFactory());

                    hub1.On("send", () => wh1.TrySetResult(null));
                    hub2.On("send", () => wh2.TrySetResult(null));

                    await hub1.Invoke("JoinGroup", "group1").OrTimeout();
                    await hub2.Invoke("JoinGroup", "group2").OrTimeout();

                    await hub1.Invoke("SendToGroups", new List<string> { "group1", "group2" }).OrTimeout();

                    await wh1.Task.OrTimeout();
                    await wh2.Task.OrTimeout();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task SendToAllButCallerInGroups(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                using (connection1)
                using (connection2)
                {
                    var tcs1 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var tcs2 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    var hub1 = connection1.CreateHubProxy("SendToSome");
                    var hub2 = connection2.CreateHubProxy("SendToSome");

                    await connection1.Start(host.TransportFactory());
                    await connection2.Start(host.TransportFactory());

                    hub1.On("send", () => tcs1.TrySetResult(null));
                    hub2.On("send", () => tcs2.TrySetResult(null));

                    await hub1.Invoke("JoinGroup", "group1").OrTimeout();
                    await hub2.Invoke("JoinGroup", "group2").OrTimeout();

                    await hub1.Invoke("AllInGroupsButCaller", new List<string> { "group1", "group2" }).OrTimeout();

                    await tcs2.Task.OrTimeout();
                    Assert.False(tcs1.Task.IsCompleted);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task JoinAndSendToGroupRenamedHub(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(enableAutoRejoiningGroups: true, messageBusType: messageBusType);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var hub = connection.CreateHubProxy("groupChat");

                    var list = new List<int>();

                    hub.On<int>("send", list.Add);

                    await connection.Start(host.Transport);

                    await hub.Invoke("Join", "Foo").OrTimeout();

                    await Task.Delay(100);

                    await hub.Invoke("Send", "Foo", "1").OrTimeout();

                    await Task.Delay(100);

                    await hub.Invoke("Leave", "Foo").OrTimeout();

                    await Task.Delay(100);

                    for (var i = 0; i < 10; i++)
                    {
                        await hub.Invoke("Send", "Foo", "2").OrTimeout();
                    }

                    Assert.Equal(1, list.Count);
                    Assert.Equal(1, list[0]);
                }
            }
        }

        [Theory]
        [InlineData(TransportType.LongPolling)]
        [InlineData(TransportType.ServerSentEvents)]
        public async Task CanSuppressExceptionsInHubPipelineModuleOnIncomingError(TransportType transportType)
        {
            var supressErrorModule = new SuppressErrorModule();
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR(config);
                    config.Resolver.Resolve<IHubPipeline>().AddModule(supressErrorModule);
                });

                using (var connection = CreateHubConnection("http://foo/"))
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(CreateTransport(transportType, host));

                    Assert.Equal(42, await hub.Invoke<int>("TaskWithException"));
                }
            }
        }

        [Theory]
        [InlineData(TransportType.LongPolling)]
        [InlineData(TransportType.ServerSentEvents)]
        public async Task CanChangeExceptionsInHubPipelineModuleOnIncomingError(TransportType transportType)
        {
            var supressErrorModule = new WrapErrorModule();
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };
                    app.MapSignalR(config);
                    config.Resolver.Resolve<IHubPipeline>().AddModule(supressErrorModule);
                });

                using (var connection = CreateHubConnection("http://foo/"))
                {
                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(CreateTransport(transportType, host));

                    try
                    {
                        await hub.Invoke("TaskWithException");
                    }
                    catch (Client.HubException ex)
                    {
                        Assert.Equal("Wrapped", ex.Message);
                        Assert.Equal("Data", (string)((dynamic)ex.ErrorData).Error);
                        return;
                    }

                    Assert.True(false, "hub.Invoke didn't throw.");
                }
            }
        }
        internal static HubConnection GetUserConnection(string userName)
        {
            var qs = new Dictionary<string, string>
            {
                { "name", userName }
            };

            return new HubConnection("http://memoryhost", qs);
        }

        internal static IHubContext<IBasicClient> InitializeUserByQuerystring(MemoryHost host)
        {
            IHubContext<IBasicClient> hubContext = null;
            host.Configure(app =>
            {
                var configuration = new HubConfiguration
                {
                    Resolver = new DefaultDependencyResolver()
                };

                var provider = new Mock<IUserIdProvider>();
                provider.Setup(m => m.GetUserId(It.IsAny<IRequest>()))
                        .Returns<IRequest>(request =>
                        {
                            return request.QueryString["name"];
                        });

                configuration.Resolver.Register(typeof(IUserIdProvider), () => provider.Object);

                app.MapSignalR(configuration);
                hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext<SendToSome, IBasicClient>();
            });

            return hubContext;
        }

        public class SomeHub : Hub
        {
            public void AllFoo()
            {
                Clients.All.foo();
            }

            public void OneFoo()
            {
                Clients.Caller.foo();
            }
        }

        public class CustomQueryHub : Hub
        {
            public string GetQueryString(string key)
            {
                return Context.QueryString[key];
            }
        }

        public class MultGroupHub : Hub
        {
            public virtual async Task Login(User user)
            {
                await Groups.Remove(Context.ConnectionId, "foo").OrTimeout();
                await Groups.Add(Context.ConnectionId, "foo").OrTimeout();

                await Groups.Remove(Context.ConnectionId, user.Name).OrTimeout();
                await Groups.Add(Context.ConnectionId, user.Name).OrTimeout();
            }

            public async Task JoinRoom(User user)
            {
                // De-dynamicize the Task so we can call OrTimeout.
                Task task = Clients.Group(user.Name).onRoomJoin(user);
                await task.OrTimeout();
            }
        }

        public class MultGroupHub2 : MultGroupHub
        {
            public override async Task Login(User user)
            {
                await Groups.Remove(Context.ConnectionId, "foo2").OrTimeout();
                await Groups.Add(Context.ConnectionId, "foo2").OrTimeout();

                await Groups.Remove(Context.ConnectionId, user.Name + "2").OrTimeout();
                await Groups.Add(Context.ConnectionId, user.Name + "2").OrTimeout();
            }
        }

        public class LogRejoiningGroupsModule : HubPipelineModule
        {
            public Dictionary<string, List<string>> GroupsRejoined = new Dictionary<string, List<string>>();

            public override Func<HubDescriptor, IRequest, IList<string>, IList<string>> BuildRejoiningGroups(Func<HubDescriptor, IRequest, IList<string>, IList<string>> rejoiningGroups)
            {
                return (hubDescriptor, request, groups) =>
                {
                    if (!GroupsRejoined.ContainsKey(hubDescriptor.Name))
                    {
                        GroupsRejoined[hubDescriptor.Name] = new List<string>(groups);
                    }
                    else
                    {
                        GroupsRejoined[hubDescriptor.Name].AddRange(groups);
                    }
                    return groups;
                };
            }
        }

        public class SuppressErrorModule : HubPipelineModule
        {
            protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
            {
                exceptionContext.Result = 42;
            }
        }

        public class WrapErrorModule : HubPipelineModule
        {
            protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
            {
                exceptionContext.Error = new HubException("Wrapped", new { Error = "Data" });
            }
        }

        public class User
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public string Room { get; set; }
        }
    }

    public class SendToSome : Hub
    {
        public Task SendToUser(string userId)
        {
            return Clients.User(userId).send();
        }

        public Task SendToUsers(IList<string> userIds)
        {
            return Clients.Users(userIds).send();
        }

        public Task SendToAllButCaller()
        {
            return Clients.Others.send();
        }

        public Task SendToAll()
        {
            return Clients.All.send();
        }

        public Task JoinGroup(string group)
        {
            return Groups.Add(Context.ConnectionId, group);
        }

        public Task SendToGroup(string group)
        {
            return Clients.Group(group).send();
        }

        public Task AllInGroupButCaller(string group)
        {
            return Clients.OthersInGroup(group).send();
        }

        public Task SendToSelf()
        {
            return Clients.Client(Context.ConnectionId).send();
        }

        public Task SendToConnections(IList<string> connectionIds)
        {
            return Clients.Clients(connectionIds).send();
        }

        public Task SendToGroups(IList<string> groups)
        {
            return Clients.Groups(groups).send();
        }

        public Task AllInGroupsButCaller(IList<string> groups)
        {
            return Clients.OthersInGroups(groups).send();
        }
    }


    public interface IBasicClient
    {
        void send();
    }

    public class SendToSomeTyped : Hub<IBasicClient>
    {
        public void SendToUsers(IList<string> userIds)
        {
            Clients.Users(userIds).send();
        }
    }
}
