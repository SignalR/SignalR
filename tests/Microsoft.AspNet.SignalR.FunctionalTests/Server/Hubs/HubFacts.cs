using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Utilities;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void ReadingState(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                hub["name"] = "test";

                connection.Start(host.Transport).Wait();

                var result = hub.InvokeWithTimeout<string>("ReadStateValue");

                Assert.Equal("test", result);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void VerifyOwinContext(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = new Client.Hubs.HubConnection(host.Url);
                var connection2 = new Client.Hubs.HubConnection(host.Url);

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

                connection.Start(host.Transport).Wait();
                connection2.Start(host.Transport).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(2));

                hub1.InvokeWithTimeout("GetItems");

                Thread.Sleep(TimeSpan.FromSeconds(2));

                connection.Stop();

                Thread.Sleep(TimeSpan.FromSeconds(2));

                Debug.WriteLine(String.Join(", ", results));

                Assert.Equal(3, results.Count);
                Assert.Equal("OnConnected", results[0].Method);
                Assert.Equal(1, results[0].Keys.Length);
                Assert.Equal("owin.environment", results[0].Keys[0]);
                Assert.Equal("GetItems", results[1].Method);
                Assert.Equal(1, results[1].Keys.Length);
                Assert.Equal("owin.environment", results[1].Keys[0]);
                Assert.Equal("OnDisconnected", results[2].Method);
                Assert.Equal(1, results[2].Keys.Length);
                Assert.Equal("owin.environment", results[2].Keys[0]);

                connection2.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void SettingState(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");
                connection.Start(host.Transport).Wait();

                var result = hub.InvokeWithTimeout<string>("SetStateValue", "test");

                Assert.Equal("test", result);
                Assert.Equal("test", hub["Company"]);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void GetValueFromServer(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.Transport).Wait();

                var result = hub.InvokeWithTimeout<int>("GetValue");

                Assert.Equal(10, result);
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void TaskWithException(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.Transport).Wait();

                var ex = Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("TaskWithException"));

                Assert.IsType<InvalidOperationException>(ex.GetBaseException());
                Assert.Contains("System.Exception", ex.GetBaseException().Message);
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void GenericTaskWithException(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.Transport).Wait();

                var ex = Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("GenericTaskWithException"));

                Assert.IsType<InvalidOperationException>(ex.GetBaseException());
                Assert.Contains("System.Exception", ex.GetBaseException().Message);
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void GenericTaskWithContinueWith(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.Transport).Wait();

                int result = hub.InvokeWithTimeout<int>("GenericTaskWithContinueWith");

                Assert.Equal(4, result);
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void Overloads(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.Transport).Wait();

                hub.InvokeWithTimeout("Overload");
                int n = hub.InvokeWithTimeout<int>("Overload", 1);

                Assert.Equal(1, n);
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void UnsupportedOverloads(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.Transport).Wait();

                TestUtilities.AssertAggregateException<InvalidOperationException>(() => hub.InvokeWithTimeout("UnsupportedOverload", 13177), "'UnsupportedOverload' method could not be resolved.");

                connection.Stop();
            }
        }

        [Fact]
        public void ChangeHubUrl()
        {
            using (var host = new MemoryHost())
            {
                host.MapHubs("/foo");
                var connection = new Client.Hubs.HubConnection("http://site/foo", useDefaultUrl: false);

                var hub = connection.CreateHubProxy("demo");

                var wh = new ManualResetEventSlim(false);

                hub.On("signal", id =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("DynamicTask");

                Assert.True(wh.Wait(TimeSpan.FromSeconds(10)));
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void GuidTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                var wh = new ManualResetEventSlim(false);

                hub.On<Guid>("TestGuid", id =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host.Transport).Wait();

                hub.InvokeWithTimeout("TestGuid");

                Assert.True(wh.Wait(TimeSpan.FromSeconds(10)));
                connection.Stop();
            }
        }

        [Fact]
        public void HubHasConnectionEvents()
        {
            var type = typeof(Hub);
            // Hub has the disconnect method
            Assert.True(type.GetMethod("OnDisconnected") != null);

            // Hub has the connect method
            Assert.True(type.GetMethod("OnConnected") != null);

            // Hub has the reconnect method
            Assert.True(type.GetMethod("OnReconnected") != null);
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void ComplexPersonState(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                var hub = connection.CreateHubProxy("demo");

                var wh = new ManualResetEvent(false);

                connection.Start(host.Transport).Wait();

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

                var person1 = hub.InvokeWithTimeout<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>("ComplexType", person);
                var person2 = hub.GetValue<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>("person");
                JObject obj = ((dynamic)hub).person;
                var person3 = obj.ToObject<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>();

                Assert.NotNull(person1);
                Assert.NotNull(person2);
                Assert.NotNull(person3);
                Assert.Equal("David", person1.Name);
                Assert.Equal("David", person2.Name);
                Assert.Equal("David", person3.Name);
                Assert.Equal(25, person1.Age);
                Assert.Equal(25, person2.Age);
                Assert.Equal(25, person3.Age);
                Assert.Equal("Redmond", person1.Address.Street);
                Assert.Equal("Redmond", person2.Address.Street);
                Assert.Equal("Redmond", person3.Address.Street);
                Assert.Equal("98052", person1.Address.Zip);
                Assert.Equal("98052", person2.Address.Zip);
                Assert.Equal("98052", person3.Address.Zip);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void DynamicInvokeTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                string callback = @"!!!|\CallMeBack,,,!!!";

                var hub = connection.CreateHubProxy("demo");

                var wh = new ManualResetEventSlim(false);

                hub.On(callback, () => wh.Set());

                connection.Start(host.Transport).Wait();

                hub.InvokeWithTimeout("DynamicInvoke", callback);

                Assert.True(wh.Wait(TimeSpan.FromSeconds(10)));
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void CreateProxyAfterConnectionStartsThrows(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = new Client.Hubs.HubConnection(host.Url);

                try
                {
                    connection.Start(host.Transport).Wait();
                    Assert.Throws<InvalidOperationException>(() => connection.CreateHubProxy("demo"));
                }
                finally
                {
                    connection.Stop();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void AddingToMultipleGroups(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                int max = 10;

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                var connection = new Client.Hubs.HubConnection(host.Url);
                var proxy = connection.CreateHubProxy("MultGroupHub");

                proxy.On<User>("onRoomJoin", user =>
                {
                    Assert.True(countDown.Mark(user.Index));
                });

                connection.Start(host.Transport).Wait();

                for (int i = 0; i < max; i++)
                {
                    var user = new User { Index = i, Name = "tester", Room = "test" + i };
                    proxy.InvokeWithTimeout("login", user);
                    proxy.InvokeWithTimeout("joinRoom", user);
                }

                Assert.True(countDown.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void HubGroupsDontRejoinByDefault(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: null,
                                connectonTimeOut: 1,
                                hearbeatInterval: 1);

                int max = 10;

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                var countDownAfterReconnect = new CountDownRange<int>(Enumerable.Range(max, max));
                var connection = new Client.Hubs.HubConnection(host.Url);
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

                connection.Start(host.Transport).Wait();

                var user = new User { Name = "tester" };
                proxy.InvokeWithTimeout("login", user);

                for (int i = 0; i < max; i++)
                {
                    user.Index = i;
                    proxy.InvokeWithTimeout("joinRoom", user);
                }

                // Force Reconnect
                Thread.Sleep(TimeSpan.FromSeconds(3));

                for (int i = max; i < 2 * max; i++)
                {
                    user.Index = i;
                    proxy.InvokeWithTimeout("joinRoom", user);
                }

                Assert.True(countDown.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
                Assert.True(!countDownAfterReconnect.Wait(TimeSpan.FromSeconds(30)) && countDownAfterReconnect.Count == max);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void HubGroupsRejoinWhenAutoRejoiningGroupsEnabled(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: null,
                                connectonTimeOut: 5,
                                hearbeatInterval: 2,
                                enableAutoRejoiningGroups: true);

                int max = 10;

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                var countDownAfterReconnect = new CountDownRange<int>(Enumerable.Range(max, max));
                var connection = new Client.Hubs.HubConnection(host.Url);
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

                connection.Start(host.Transport).Wait();

                var user = new User { Name = "tester" };
                proxy.InvokeWithTimeout("login", user);

                for (int i = 0; i < max; i++)
                {
                    user.Index = i;
                    proxy.InvokeWithTimeout("joinRoom", user);
                }

                // Force Reconnect
                Thread.Sleep(TimeSpan.FromSeconds(3));

                for (int i = max; i < 2 * max; i++)
                {
                    user.Index = i;
                    proxy.InvokeWithTimeout("joinRoom", user);
                }

                Assert.True(countDown.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
                Assert.True(countDownAfterReconnect.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDownAfterReconnect.Count) + " missed " + String.Join(",", countDownAfterReconnect.Left.Select(i => i.ToString())));

                connection.Stop();
            }
        }

        [Fact]
        public void RejoiningGroupsOnlyReceivesGroupsBelongingToHub()
        {
            var logRejoiningGroups = new LogRejoiningGroupsModule();
            using (var host = new MemoryHost())
            {
                host.HubPipeline.AddModule(logRejoiningGroups);

                host.Configuration.KeepAlive = null;
                host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
                host.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(1);
                host.MapHubs();

                var connection = new Client.Hubs.HubConnection("http://foo");
                var proxy = connection.CreateHubProxy("MultGroupHub");
                var proxy2 = connection.CreateHubProxy("MultGroupHub2");

                connection.Start(host).Wait();

                var user = new User { Name = "tester" };
                proxy.InvokeWithTimeout("login", user);
                proxy2.InvokeWithTimeout("login", user);

                // Force Reconnect
                Thread.Sleep(TimeSpan.FromSeconds(3));

                proxy.InvokeWithTimeout("joinRoom", user);
                proxy2.InvokeWithTimeout("joinRoom", user);

                Thread.Sleep(TimeSpan.FromSeconds(3));

                Assert.True(logRejoiningGroups.GroupsRejoined["MultGroupHub"].Contains("foo"));
                Assert.True(logRejoiningGroups.GroupsRejoined["MultGroupHub"].Contains("tester"));
                Assert.False(logRejoiningGroups.GroupsRejoined["MultGroupHub"].Contains("foo2"));
                Assert.False(logRejoiningGroups.GroupsRejoined["MultGroupHub"].Contains("tester2"));
                Assert.True(logRejoiningGroups.GroupsRejoined["MultGroupHub2"].Contains("foo2"));
                Assert.True(logRejoiningGroups.GroupsRejoined["MultGroupHub2"].Contains("tester2"));
                Assert.False(logRejoiningGroups.GroupsRejoined["MultGroupHub2"].Contains("foo"));
                Assert.False(logRejoiningGroups.GroupsRejoined["MultGroupHub2"].Contains("tester"));

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void CustomQueryStringRaw(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = new Client.Hubs.HubConnection(host.Url, "a=b");

                var hub = connection.CreateHubProxy("CustomQueryHub");

                connection.Start(host.Transport).Wait();

                var result = hub.InvokeWithTimeout<string>("GetQueryString", "a");

                Assert.Equal("b", result);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void CustomQueryString(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var qs = new Dictionary<string, string>();
                qs["a"] = "b";
                var connection = new Client.Hubs.HubConnection(host.Url, qs);

                var hub = connection.CreateHubProxy("CustomQueryHub");

                connection.Start(host.Transport).Wait();

                var result = hub.InvokeWithTimeout<string>("GetQueryString", "a");

                Assert.Equal("b", result);

                connection.Stop();
            }
        }

        [Fact]
        public void ReturningNullFromConnectAndDisconnectAccepted()
        {
            var mockHub = new Mock<SomeHub>() { CallBase = true };
            mockHub.Setup(h => h.OnConnected()).Returns<Task>(null).Verifiable();
            mockHub.Setup(h => h.OnDisconnected()).Returns<Task>(null).Verifiable();

            using (var host = new MemoryHost())
            {
                host.HubPipeline.EnableAutoRejoiningGroups();
                host.Configuration.KeepAlive = null;
                host.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(1);
                host.DependencyResolver.Register(typeof(SomeHub), () => mockHub.Object);
                host.MapHubs();
                var connection = new Client.Hubs.HubConnection("http://foo");

                var hub = connection.CreateHubProxy("SomeHub");
                connection.Start(host).Wait();

                connection.Stop();
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }

            mockHub.Verify();
        }

        [Fact]
        public void ReturningNullFromReconnectAccepted()
        {
            var mockHub = new Mock<SomeHub>() { CallBase = true };
            mockHub.Setup(h => h.OnReconnected()).Returns<Task>(null).Verifiable();

            using (var host = new MemoryHost())
            {
                host.HubPipeline.EnableAutoRejoiningGroups();
                host.Configuration.KeepAlive = null;
                host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
                host.Configuration.HeartbeatInterval = TimeSpan.FromSeconds(1);
                host.DependencyResolver.Register(typeof(SomeHub), () => mockHub.Object);
                host.MapHubs();
                var connection = new Client.Hubs.HubConnection("http://foo");

                var hub = connection.CreateHubProxy("SomeHub");
                connection.Start(host).Wait();

                // Force Reconnect
                Thread.Sleep(TimeSpan.FromSeconds(3));

                hub.InvokeWithTimeout("AllFoo");

                Thread.Sleep(TimeSpan.FromSeconds(3));

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
        public void CreatedHubsGetDisposed()
        {
            var mockHubs = new List<Mock<IHub>>();

            using (var host = new MemoryHost())
            {
                host.DependencyResolver.Register(typeof(IHub), () =>
                {
                    var mockHub = new Mock<IHub>() { CallBase = true };

                    mockHubs.Add(mockHub);
                    return mockHub.Object;
                });
                host.MapHubs();
                var connection = new Client.Hubs.HubConnection("http://foo/");

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host).Wait();

                var result = hub.InvokeWithTimeout<string>("ReadStateValue");

                foreach (var mockDemoHub in mockHubs)
                {
                    mockDemoHub.Verify(d => d.Dispose(), Times.Once());
                }

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void SendToAllButCaller(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = new Client.Hubs.HubConnection(host.Url);
                var connection2 = new Client.Hubs.HubConnection(host.Url);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.Transport).Wait();
                connection2.Start(host.Transport).Wait();

                hub1.On("send", wh1.Set);
                hub2.On("send", wh2.Set);

                hub1.InvokeWithTimeout("SendToAllButCaller");

                Assert.False(wh1.Wait(TimeSpan.FromSeconds(5)));
                Assert.True(wh2.Wait(TimeSpan.FromSeconds(10)));

                connection1.Stop();
                connection2.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void SendToAllButCallerInGroup(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = new Client.Hubs.HubConnection(host.Url);
                var connection2 = new Client.Hubs.HubConnection(host.Url);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.Transport).Wait();
                connection2.Start(host.Transport).Wait();

                hub1.On("send", wh1.Set);
                hub2.On("send", wh2.Set);

                hub1.InvokeWithTimeout("JoinGroup", "group");
                hub2.InvokeWithTimeout("JoinGroup", "group");

                hub1.InvokeWithTimeout("AllInGroupButCaller", "group");

                Assert.False(wh1.Wait(TimeSpan.FromSeconds(10)));
                Assert.True(wh2.Wait(TimeSpan.FromSeconds(5)));

                connection1.Stop();
                connection2.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void SendToAll(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = new Client.Hubs.HubConnection(host.Url);
                var connection2 = new Client.Hubs.HubConnection(host.Url);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.Transport).Wait();
                connection2.Start(host.Transport).Wait();

                hub1.On("send", wh1.Set);
                hub2.On("send", wh2.Set);

                hub1.InvokeWithTimeout("SendToAll");

                Assert.True(wh1.Wait(TimeSpan.FromSeconds(10)));
                Assert.True(wh2.Wait(TimeSpan.FromSeconds(10)));

                connection1.Stop();
                connection2.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void SendToSelf(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = new Client.Hubs.HubConnection(host.Url);
                var connection2 = new Client.Hubs.HubConnection(host.Url);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.Transport).Wait();
                connection2.Start(host.Transport).Wait();

                hub1.On("send", wh1.Set);
                hub2.On("send", wh2.Set);

                hub1.InvokeWithTimeout("SendToSelf");

                Assert.True(wh1.Wait(TimeSpan.FromSeconds(10)));
                Assert.False(wh2.Wait(TimeSpan.FromSeconds(5)));

                connection1.Stop();
                connection2.Stop();
            }
        }

        [Fact]
        public void SendToGroupFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                host.MapHubs();
                var connection1 = new Client.Hubs.HubConnection("http://foo/");
                var hubContext = host.ConnectionManager.GetHubContext("SendToSome");

                var wh1 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");

                connection1.Start(host).Wait();

                hub1.On("send", wh1.Set);

                hubContext.Groups.Add(connection1.ConnectionId, "Foo").Wait();
                hubContext.Clients.Group("Foo").send();

                Assert.True(wh1.Wait(TimeSpan.FromSeconds(10)));

                connection1.Stop();
            }
        }

        [Fact]
        public void SendToSpecificClientFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                host.MapHubs();
                var connection1 = new Client.Hubs.HubConnection("http://foo/");
                var hubContext = host.ConnectionManager.GetHubContext("SendToSome");

                var wh1 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");

                connection1.Start(host).Wait();

                hub1.On("send", wh1.Set);

                hubContext.Clients.Client(connection1.ConnectionId).send();

                Assert.True(wh1.Wait(TimeSpan.FromSeconds(10)));

                connection1.Stop();
            }
        }

        [Fact]
        public void SendToAllFromOutsideOfHub()
        {
            using (var host = new MemoryHost())
            {
                host.MapHubs();
                var connection1 = new Client.Hubs.HubConnection("http://foo/");
                var connection2 = new Client.Hubs.HubConnection("http://foo/");
                var hubContext = host.ConnectionManager.GetHubContext("SendToSome");

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host).Wait();
                connection2.Start(host).Wait();

                hub1.On("send", wh1.Set);
                hub2.On("send", wh2.Set);

                hubContext.Clients.All.send();

                Assert.True(wh1.Wait(TimeSpan.FromSeconds(10)));
                Assert.True(wh2.Wait(TimeSpan.FromSeconds(10)));

                connection1.Stop();
                connection2.Stop();
            }
        }

        public class SendToSome : Hub
        {
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

            public Task AllInGroupButCaller(string group)
            {
                return Clients.OthersInGroup(group).send();
            }

            public Task SendToSelf()
            {
                return Clients.Client(Context.ConnectionId).send();
            }
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
            public virtual Task Login(User user)
            {
                return Task.Factory.StartNew(
                    () =>
                    {
                        Groups.Remove(Context.ConnectionId, "foo").Wait();
                        Groups.Add(Context.ConnectionId, "foo").Wait();

                        Groups.Remove(Context.ConnectionId, user.Name).Wait();
                        Groups.Add(Context.ConnectionId, user.Name).Wait();
                    });
            }

            public Task JoinRoom(User user)
            {
                return Task.Factory.StartNew(
                    () =>
                    {
                        Clients.Group(user.Name).onRoomJoin(user).Wait();
                    });
            }
        }

        public class MultGroupHub2 : MultGroupHub
        {
            public override Task Login(User user)
            {
                return Task.Factory.StartNew(
                    () =>
                    {
                        Groups.Remove(Context.ConnectionId, "foo2").Wait();
                        Groups.Add(Context.ConnectionId, "foo2").Wait();

                        Groups.Remove(Context.ConnectionId, user.Name + "2").Wait();
                        Groups.Add(Context.ConnectionId, user.Name + "2").Wait();
                    });
            }
        }

        public class LogRejoiningGroupsModule : HubPipelineModule
        {
            public Dictionary<string, List<string>> GroupsRejoined = new Dictionary<string, List<string>>();

            public override Func<HubDescriptor, IRequest, IEnumerable<string>, IEnumerable<string>> BuildRejoiningGroups(Func<HubDescriptor, IRequest, IEnumerable<string>, IEnumerable<string>> rejoiningGroups)
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

        public class User
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public string Room { get; set; }
        }
    }
}
