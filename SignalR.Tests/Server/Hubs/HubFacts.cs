using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Moq;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;
using SignalR.Hubs;
using SignalR.Tests.Infrastructure;
using Xunit;

namespace SignalR.Tests
{
    public class HubFacts : IDisposable
    {
        [Fact]
        public void ReadingState()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            hub["name"] = "test";

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("ReadStateValue").Result;

            Assert.Equal("test", result);

            connection.Stop();
        }

        [Fact]
        public void SettingState()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");
            connection.Start(host).Wait();

            var result = hub.Invoke<string>("SetStateValue", "test").Result;

            Assert.Equal("test", result);
            Assert.Equal("test", hub["Company"]);

            connection.Stop();
        }

        [Fact]
        public void GetValueFromServer()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var result = hub.Invoke<int>("GetValue").Result;

            Assert.Equal(10, result);
            connection.Stop();
        }

        [Fact]
        public void TaskWithException()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<AggregateException>(() => hub.Invoke("TaskWithException").Wait());

            Assert.IsType<InvalidOperationException>(ex.GetBaseException());
            Assert.Contains("System.Exception", ex.GetBaseException().Message);
            connection.Stop();
        }

        [Fact]
        public void GenericTaskWithException()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<AggregateException>(() => hub.Invoke("GenericTaskWithException").Wait());

            Assert.IsType<InvalidOperationException>(ex.GetBaseException());
            Assert.Contains("System.Exception", ex.GetBaseException().Message);
            connection.Stop();
        }

        [Fact]
        public void GenericTaskWithContinueWith()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            int result = hub.Invoke<int>("GenericTaskWithContinueWith").Result;

            Assert.Equal(4, result);
            connection.Stop();
        }

        [Fact]
        public void Overloads()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            hub.Invoke("Overload").Wait();
            int n = hub.Invoke<int>("Overload", 1).Result;

            Assert.Equal(1, n);
            connection.Stop();
        }

        [Fact]
        public void UnsupportedOverloads()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<InvalidOperationException>(() => hub.Invoke("UnsupportedOverload", 13177).Wait());

            Assert.Equal("'UnsupportedOverload' method could not be resolved.", ex.GetBaseException().Message);
            connection.Stop();
        }

        [Fact]
        public void ChangeHubUrl()
        {
            var host = new MemoryHost();
            host.MapHubs("/foo");
            var connection = new Client.Hubs.HubConnection("http://site/foo", useDefaultUrl: false);

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            hub.On("signal", id =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("DynamicTask").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            connection.Stop();
        }

        [Fact]
        public void GuidTest()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            hub.On<Guid>("TestGuid", id =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("TestGuid").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            connection.Stop();
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

        [Fact]
        public void ComplexPersonState()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            connection.Start(host).Wait();

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

            var person1 = hub.Invoke<SignalR.Samples.Hubs.DemoHub.DemoHub.Person>("ComplexType", person).Result;
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

        [Fact]
        public void DynamicInvokeTest()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");
            string callback = @"!!!|\CallMeBack,,,!!!";

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            hub.On(callback, () => wh.Set());

            connection.Start(host).Wait();

            hub.Invoke("DynamicInvoke", callback).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            connection.Stop();
        }

        [Fact]
        public void CreateProxyAfterConnectionStartsThrows()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://site/");

            try
            {
                connection.Start(host).Wait();
                Assert.Throws<InvalidOperationException>(() => connection.CreateProxy("demo"));
            }
            finally
            {
                connection.Stop();
            }
        }

        [Fact]
        public void AddingToMultipleGroups()
        {
            var host = new MemoryHost();
            host.MapHubs();
            int max = 10;

            var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateProxy("MultGroupHub");

            proxy.On<User>("onRoomJoin", user =>
            {
                Assert.True(countDown.Mark(user.Index));
            });

            connection.Start(host).Wait();

            for (int i = 0; i < max; i++)
            {
                var user = new User { Index = i, Name = "tester", Room = "test" + i };
                proxy.Invoke("login", user).Wait();
                proxy.Invoke("joinRoom", user).Wait();
            }

            Assert.True(countDown.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));

            connection.Stop();
        }

        [Fact]
        public void HubGroupsDontRejoinByDefault()
        {
            var host = new MemoryHost();
            host.Configuration.KeepAlive = null;
            host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
            host.MapHubs();
            int max = 10;

            var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
            var countDownAfterReconnect = new CountDownRange<int>(Enumerable.Range(max, max));
            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateProxy("MultGroupHub");

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

            connection.Start(host).Wait();

            var user = new User { Name = "tester" };
            proxy.Invoke("login", user).Wait();

            for (int i = 0; i < max; i++)
            {
                user.Index = i;
                proxy.Invoke("joinRoom", user).Wait();
            }

            // Force Reconnect
            Thread.Sleep(TimeSpan.FromSeconds(3));

            for (int i = max; i < 2 * max; i++)
            {
                user.Index = i;
                proxy.Invoke("joinRoom", user).Wait();
            }

            Assert.True(countDown.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
            Assert.True(!countDownAfterReconnect.Wait(TimeSpan.FromSeconds(30)) && countDownAfterReconnect.Count == max);

            connection.Stop();
        }

        [Fact]
        public void HubGroupsRejoinWhenRejoiningGroupsOverridden()
        {
            var host = new MemoryHost();
            host.Configuration.KeepAlive = null;
            host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
            host.MapHubs();
            int max = 10;

            var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
            var countDownAfterReconnect = new CountDownRange<int>(Enumerable.Range(max, max));
            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateProxy("RejoinMultGroupHub");

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

            connection.Start(host).Wait();

            var user = new User { Name = "tester" };
            proxy.Invoke("login", user).Wait();

            for (int i = 0; i < max; i++)
            {
                user.Index = i;
                proxy.Invoke("joinRoom", user).Wait();
            }

            // Force Reconnect
            Thread.Sleep(TimeSpan.FromSeconds(3));

            for (int i = max; i < 2 * max; i++)
            {
                user.Index = i;
                proxy.Invoke("joinRoom", user).Wait();
            }

            Assert.True(countDown.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
            Assert.True(countDownAfterReconnect.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDownAfterReconnect.Count) + " missed " + String.Join(",", countDownAfterReconnect.Left.Select(i => i.ToString())));

            connection.Stop();
        }

        [Fact]
        public void HubGroupsRejoinWhenAutoRejoiningGroupsEnabled()
        {
            var host = new MemoryHost();
            host.HubPipeline.EnableAutoRejoiningGroups();
            host.Configuration.KeepAlive = null;
            host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
            host.MapHubs();
            int max = 10;

            var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
            var countDownAfterReconnect = new CountDownRange<int>(Enumerable.Range(max, max));
            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateProxy("MultGroupHub");

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

            connection.Start(host).Wait();

            var user = new User { Name = "tester" };
            proxy.Invoke("login", user).Wait();

            for (int i = 0; i < max; i++)
            {
                user.Index = i;
                proxy.Invoke("joinRoom", user).Wait();
            }

            // Force Reconnect
            Thread.Sleep(TimeSpan.FromSeconds(3));

            for (int i = max; i < 2 * max; i++)
            {
                user.Index = i;
                proxy.Invoke("joinRoom", user).Wait();
            }

            Assert.True(countDown.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDown.Count) + " missed " + String.Join(",", countDown.Left.Select(i => i.ToString())));
            Assert.True(countDownAfterReconnect.Wait(TimeSpan.FromSeconds(30)), "Didn't receive " + max + " messages. Got " + (max - countDownAfterReconnect.Count) + " missed " + String.Join(",", countDownAfterReconnect.Left.Select(i => i.ToString())));

            connection.Stop();
        }

        [Fact]
        public void RejoiningGroupsOnlyReceivesGroupsBelongingToHub()
        {
            var host = new MemoryHost();
            var groupsRequestedToBeRejoined = new List<string>();
            var groupsRequestedToBeRejoined2 = new List<string>();
            host.DependencyResolver.Register(typeof(RejoinMultGroupHub), () => new RejoinMultGroupHub(groupsRequestedToBeRejoined));
            host.DependencyResolver.Register(typeof(RejoinMultGroupHub2), () => new RejoinMultGroupHub2(groupsRequestedToBeRejoined2));
            host.Configuration.KeepAlive = null;
            host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
            host.MapHubs();

            var connection = new Client.Hubs.HubConnection("http://foo");
            var proxy = connection.CreateProxy("RejoinMultGroupHub");
            var proxy2 = connection.CreateProxy("RejoinMultGroupHub2");

            connection.Start(host).Wait();

            var user = new User { Name = "tester" };
            proxy.Invoke("login", user).Wait();
            proxy2.Invoke("login", user).Wait();

            // Force Reconnect
            Thread.Sleep(TimeSpan.FromSeconds(3));

            proxy.Invoke("joinRoom", user).Wait();
            proxy2.Invoke("joinRoom", user).Wait();

            Thread.Sleep(TimeSpan.FromSeconds(3));

            Assert.True(groupsRequestedToBeRejoined.Contains("foo"));
            Assert.True(groupsRequestedToBeRejoined.Contains("tester"));
            Assert.False(groupsRequestedToBeRejoined.Contains("foo2"));
            Assert.False(groupsRequestedToBeRejoined.Contains("tester2"));
            Assert.True(groupsRequestedToBeRejoined2.Contains("foo2"));
            Assert.True(groupsRequestedToBeRejoined2.Contains("tester2"));
            Assert.False(groupsRequestedToBeRejoined2.Contains("foo"));
            Assert.False(groupsRequestedToBeRejoined2.Contains("tester"));

            connection.Stop();
        }

        [Fact]
        public void CustomQueryStringRaw()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/", "a=b");

            var hub = connection.CreateProxy("CustomQueryHub");

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("GetQueryString", "a").Result;

            Assert.Equal("b", result);

            connection.Stop();
        }

        [Fact]
        public void CustomQueryString()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var qs = new Dictionary<string, string>();
            qs["a"] = "b";
            var connection = new Client.Hubs.HubConnection("http://foo/", qs);

            var hub = connection.CreateProxy("CustomQueryHub");

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("GetQueryString", "a").Result;

            Assert.Equal("b", result);

            connection.Stop();
        }

        [Fact]
        public void ReturningNullFromConnectAndDisconnectAccepted()
        {
            var mockHub = new Mock<SomeHub>() { CallBase = true };
            mockHub.Setup(h => h.OnConnected()).Returns<Task>(null).Verifiable();
            mockHub.Setup(h => h.OnDisconnected()).Returns<Task>(null).Verifiable();

            var host = new MemoryHost();
            host.HubPipeline.EnableAutoRejoiningGroups();
            host.Configuration.KeepAlive = null;
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
            host.DependencyResolver.Register(typeof(SomeHub), () => mockHub.Object);
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo");

            var hub = connection.CreateProxy("SomeHub");
            connection.Start(host).Wait();

            connection.Stop();
            Thread.Sleep(TimeSpan.FromSeconds(3));

            mockHub.Verify();
        }

        [Fact]
        public void ReturningNullFromReconnectAccepted()
        {
            var mockHub = new Mock<SomeHub>() { CallBase = true };
            mockHub.Setup(h => h.OnReconnected()).Returns<Task>(null).Verifiable();

            var host = new MemoryHost();
            host.HubPipeline.EnableAutoRejoiningGroups();
            host.Configuration.KeepAlive = null;
            host.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(1);
            host.Configuration.HeartBeatInterval = TimeSpan.FromSeconds(1);
            host.DependencyResolver.Register(typeof(SomeHub), () => mockHub.Object);
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo");

            var hub = connection.CreateProxy("SomeHub");
            connection.Start(host).Wait();

            // Force Reconnect
            Thread.Sleep(TimeSpan.FromSeconds(3));

            hub.Invoke("AllFoo").Wait();

            Thread.Sleep(TimeSpan.FromSeconds(3));

            connection.Stop();

            mockHub.Verify();
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
            var mockDemoHubs = new List<Mock<SignalR.Samples.Hubs.DemoHub.DemoHub>>();

            var host = new MemoryHost();
            host.DependencyResolver.Register(typeof(SignalR.Samples.Hubs.DemoHub.DemoHub), () =>
            {
                var mockDemoHub = new Mock<SignalR.Samples.Hubs.DemoHub.DemoHub>() { CallBase = true };
                mockDemoHubs.Add(mockDemoHub);
                return mockDemoHub.Object;
            });
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("ReadStateValue").Result;

            foreach (var mockDemoHub in mockDemoHubs)
            {
                mockDemoHub.Verify(d => d.Dispose(), Times.Once());
            }

            connection.Stop();
        }

        [Fact]
        public void SendToAllButCaller()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection1 = new Client.Hubs.HubConnection("http://foo/");
            var connection2 = new Client.Hubs.HubConnection("http://foo/");

            var wh1 = new ManualResetEventSlim(initialState: false);
            var wh2 = new ManualResetEventSlim(initialState: false);

            var hub1 = connection1.CreateProxy("SendToSome");
            var hub2 = connection2.CreateProxy("SendToSome");

            connection1.Start(host).Wait();
            connection2.Start(host).Wait();

            hub1.On("send", wh1.Set);
            hub2.On("send", wh2.Set);

            hub1.Invoke("SendToAllButCaller").Wait();

            Assert.False(wh1.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.True(wh2.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));

            connection1.Stop();
            connection2.Stop();
        }

        [Fact]
        public void SendToAllButCallerInGroup()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection1 = new Client.Hubs.HubConnection("http://foo/");
            var connection2 = new Client.Hubs.HubConnection("http://foo/");

            var wh1 = new ManualResetEventSlim(initialState: false);
            var wh2 = new ManualResetEventSlim(initialState: false);

            var hub1 = connection1.CreateProxy("SendToSome");
            var hub2 = connection2.CreateProxy("SendToSome");

            connection1.Start(host).Wait();
            connection2.Start(host).Wait();

            hub1.On("send", wh1.Set);
            hub2.On("send", wh2.Set);

            hub1.Invoke("JoinGroup", "group").Wait();
            hub2.Invoke("JoinGroup", "group").Wait();

            hub1.Invoke("AllInGroupButCaller", "group").Wait();

            Assert.False(wh1.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.True(wh2.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));

            connection1.Stop();
            connection2.Stop();
        }

        [Fact]
        public void SendToAll()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection1 = new Client.Hubs.HubConnection("http://foo/");
            var connection2 = new Client.Hubs.HubConnection("http://foo/");

            var wh1 = new ManualResetEventSlim(initialState: false);
            var wh2 = new ManualResetEventSlim(initialState: false);

            var hub1 = connection1.CreateProxy("SendToSome");
            var hub2 = connection2.CreateProxy("SendToSome");

            connection1.Start(host).Wait();
            connection2.Start(host).Wait();

            hub1.On("send", wh1.Set);
            hub2.On("send", wh2.Set);

            hub1.Invoke("SendToAll").Wait();

            Assert.True(wh1.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.True(wh2.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));

            connection1.Stop();
            connection2.Stop();
        }

        [Fact]
        public void SendToSelf()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection1 = new Client.Hubs.HubConnection("http://foo/");
            var connection2 = new Client.Hubs.HubConnection("http://foo/");

            var wh1 = new ManualResetEventSlim(initialState: false);
            var wh2 = new ManualResetEventSlim(initialState: false);

            var hub1 = connection1.CreateProxy("SendToSome");
            var hub2 = connection2.CreateProxy("SendToSome");

            connection1.Start(host).Wait();
            connection2.Start(host).Wait();

            hub1.On("send", wh1.Set);
            hub2.On("send", wh2.Set);

            hub1.Invoke("SendToSelf").Wait();

            Assert.True(wh1.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.False(wh2.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));

            connection1.Stop();
            connection2.Stop();
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

        public class RejoinMultGroupHub : MultGroupHub
        {
            private List<string> _groupsRequestedToBeRejoined;

            public RejoinMultGroupHub() : this(new List<string>()) { }

            public RejoinMultGroupHub(List<string> groupsRequestedToBeRejoined)
            {
                _groupsRequestedToBeRejoined = groupsRequestedToBeRejoined;
            }

            public override IEnumerable<string> RejoiningGroups(IEnumerable<string> groups)
            {
                _groupsRequestedToBeRejoined.AddRange(groups);
                return groups;
            }
        }

        public class RejoinMultGroupHub2 : RejoinMultGroupHub
        {
            public RejoinMultGroupHub2() : this(new List<string>()) { }

            public RejoinMultGroupHub2(List<string> groupsRequestedToBeRejoined) : base(groupsRequestedToBeRejoined) { }

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

        public class User
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public string Room { get; set; }
        }

        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
