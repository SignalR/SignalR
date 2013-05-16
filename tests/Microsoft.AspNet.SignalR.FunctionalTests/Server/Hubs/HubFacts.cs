using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.FunctionalTests.Owin;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Utilities;
using Moq;
using Newtonsoft.Json.Linq;
using Owin;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ReadingState(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("demo");

                hub["name"] = "test";

                connection.Start(host.Transport).Wait();

                var result = hub.InvokeWithTimeout<string>("ReadStateValue");

                Assert.Equal("test", result);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ReadingComplexState(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("demo");

                hub["state"] = JToken.FromObject(new
                {
                    Name = "David",
                    Address = new
                    {
                        Street = "St"
                    }
                });

                connection.Start(host.Transport).Wait();

                var result = hub.InvokeWithTimeout<dynamic>("ReadAnyState");
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

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.LongPolling)]
        // [InlineData(TransportType.Websockets)]
        public void BasicAuthCredentialsFlow(TransportType transportType)
        {
            using (var host = new OwinTestHost())
            {
                Debug.Listeners.Clear();

                host.Start<BasicAuthApplication>();

                HubConnection connection = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("demo");

                hub["name"] = "test";

                connection.Credentials = new System.Net.NetworkCredential("user", "password");

                connection.Start(CreateTransport(transportType)).Wait();

                var result = hub.InvokeWithTimeout<string>("ReadStateValue");

                Assert.Equal("test", result);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void VerifyOwinContext(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection connection = CreateHubConnection(host);
                HubConnection connection2 = CreateHubConnection(host);

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

                connection.Start(host.TransportFactory()).Wait();
                connection2.Start(host.TransportFactory()).Wait();

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
                Assert.Equal("nosniff", results[0].XContentTypeOptions);
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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void SettingState(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        public void CancelledTask(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);
                var tcs = new TaskCompletionSource<object>();

                var hub = connection.CreateHubProxy("demo");
                connection.Start(host.Transport).Wait();

                hub.Invoke("CancelledTask").ContinueWith(tcs);

                try
                {
                    tcs.Task.Wait(TimeSpan.FromSeconds(10));
                    Assert.True(false, "Didn't fault");
                }
                catch (Exception)
                {

                }

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        public void CancelledGenericTask(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);
                var tcs = new TaskCompletionSource<object>();

                var hub = connection.CreateHubProxy("demo");
                connection.Start(host.Transport).Wait();

                hub.Invoke("CancelledGenericTask").ContinueWith(tcs);

                try
                {
                    tcs.Task.Wait(TimeSpan.FromSeconds(10));
                    Assert.True(false, "Didn't fault");
                }
                catch (Exception)
                {

                }

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void GetValueFromServer(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void SynchronousException(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.Transport).Wait();

                var ex = Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("SynchronousException"));

                Assert.IsType<InvalidOperationException>(ex.GetBaseException());
                Assert.Contains("System.Exception", ex.GetBaseException().Message);
                connection.Stop();
            }
        }


        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void TaskWithException(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void GenericTaskWithException(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void DetailedErrorsAreDisabledByDefault(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var query = new Dictionary<string, string>();
                SetHostData(host, query);
                query["test"] = GetTestName();
                var connection = new Client.Hubs.HubConnection(host.Url + "/signalr2/test", useDefaultUrl: false, queryString: query);
                connection.TraceWriter = host.ClientTraceOutput;

                var hub = connection.CreateHubProxy("demo");

                connection.Start(host.TransportFactory()).Wait();

                connection.Start(host.TransportFactory()).Wait();

                var ex = Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("TaskWithException"));

                Assert.IsType<InvalidOperationException>(ex.GetBaseException());
                Assert.DoesNotContain("System.Exception", ex.GetBaseException().Message);
                Assert.Contains("demo.TaskWithException", ex.GetBaseException().Message);
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void GenericTaskWithContinueWith(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void Overloads(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void ReturnDataWithPlus(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("echoHub");

                connection.Start(host.Transport).Wait();

                string result = hub.InvokeWithTimeout<string>("EchoReturn", "+");

                Assert.Equal("+", result);
                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void CallbackDataWithPlus(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("echoHub");
                var tcs = new TaskCompletionSource<string>();
                hub.On<string>("echo", tcs.SetResult);

                connection.Start(host.Transport).Wait();

                hub.InvokeWithTimeout("EchoCallback", "+");

                Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(5)), "Timeout waiting for callback");
                Assert.Equal("+", tcs.Task.Result);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void UnsupportedOverloads(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
                host.Configure(app =>
                {
                    var config = new HubConfiguration()
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs("/foo", config);
                });

                var connection = new Client.Hubs.HubConnection("http://site/foo", useDefaultUrl: false, queryString: new Dictionary<string, string> { { "test", "ChangeHubUrl" } });


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
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ChangeHubUrlAspNet(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var query = new Dictionary<string, string> { { "test", GetTestName() } };
                SetHostData(host, query);

                var connection = new Client.Hubs.HubConnection(host.Url + "/signalr2/test", useDefaultUrl: false, queryString: query);
                connection.TraceWriter = host.ClientTraceOutput;

                var hub = connection.CreateHubProxy("demo");

                var wh = new ManualResetEventSlim(false);

                hub.On("signal", id =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host.Transport).Wait();

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
                HubConnection connection = CreateHubConnection(host);

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

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void RemainsConnectedWithHubsAppendedToUrl(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var connection = CreateHubConnection(host, host.Url + "/signalr/hubs", useDefaultUrl: false);

                var hub = connection.CreateHubProxy("demo");

                var tcs = new TaskCompletionSource<object>();
                var testGuidInvocations = 0;

                hub.On<Guid>("TestGuid", id =>
                {
                    testGuidInvocations++;
                    if (testGuidInvocations < 2)
                    {
                        hub.Invoke("TestGuid").ContinueWithNotComplete(tcs);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                });

                connection.Error += e => tcs.SetException(e);
                connection.Reconnecting += () => tcs.SetCanceled();

                connection.Start(host.Transport).Wait();

                hub.InvokeWithTimeout("TestGuid");

                Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ComplexPersonState(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

                var hub = connection.CreateHubProxy("demo");

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

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void DynamicInvokeTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void CreateProxyAfterConnectionStartsThrows(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection connection = CreateHubConnection(host);

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void AddingToMultipleGroups(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                int max = 10;

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                HubConnection connection = CreateHubConnection(host);
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
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void HubGroupsRejoinWhenAutoRejoiningGroupsEnabled(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(keepAlive: null,
                                disconnectTimeout: 6,
                                connectionTimeout: 1,
                                enableAutoRejoiningGroups: true);

                int max = 10;

                var countDown = new CountDownRange<int>(Enumerable.Range(0, max));
                var countDownAfterReconnect = new CountDownRange<int>(Enumerable.Range(max, max));
                HubConnection connection = CreateHubConnection(host);
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
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs("/signalr", config);

                    config.Resolver.Resolve<IHubPipeline>().AddModule(logRejoiningGroups);
                    var configuration = config.Resolver.Resolve<IConfigurationManager>();
                    // The following sets the heartbeat to 1 s
                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
                    configuration.KeepAlive = null;
                    configuration.ConnectionTimeout = TimeSpan.FromSeconds(2);
                });

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void CustomQueryStringRaw(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection = new Client.Hubs.HubConnection(host.Url, "a=b&test=" + GetTestName());
                connection.TraceWriter = host.ClientTraceOutput;

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void CustomQueryString(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var qs = new Dictionary<string, string>();
                qs["a"] = "b";
                qs["test"] = GetTestName();
                var connection = new Client.Hubs.HubConnection(host.Url, qs);
                connection.TraceWriter = host.ClientTraceOutput;

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
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs("/signalr", config);

                    var configuration = config.Resolver.Resolve<IConfigurationManager>();
                    // The below effectively sets the heartbeat interval to one second.
                    configuration.KeepAlive = TimeSpan.FromSeconds(2);
                    config.Resolver.Register(typeof(SomeHub), () => mockHub.Object);
                });

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
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs("/signalr", config);

                    var configuration = config.Resolver.Resolve<IConfigurationManager>();
                    // The following sets the heartbeat to 1 s
                    configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
                    configuration.KeepAlive = null;
                    configuration.ConnectionTimeout = TimeSpan.FromSeconds(2);
                    config.Resolver.Register(typeof(SomeHub), () => mockHub.Object);
                });

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
                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs("/signalr", config);

                    config.Resolver.Register(typeof(IHub), () =>
                    {
                        var mockHub = new Mock<IHub>() { CallBase = true };

                        mockHubs.Add(mockHub);
                        return mockHub.Object;
                    });
                });

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
        [InlineData(MessageBusType.Default)]
        [InlineData(MessageBusType.Fake)]
        public void JoiningGroupMultipleTimesGetsMessageOnce(MessageBusType messagebusType)
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

                    app.MapHubs(config);
                });

                var connection = new HubConnection("http://foo");

                var hub = connection.CreateHubProxy("SendToSome");
                int invocations = 0;

                connection.Start(host).Wait();

                hub.On("send", () =>
                {
                    invocations++;
                });

                // Join the group multiple times
                hub.InvokeWithTimeout("JoinGroup", "a");
                hub.InvokeWithTimeout("JoinGroup", "a");
                hub.InvokeWithTimeout("JoinGroup", "a");
                hub.InvokeWithTimeout("SendToGroup", "a");

                Thread.Sleep(TimeSpan.FromSeconds(3));

                Assert.Equal(1, invocations);

                connection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void SendToAllButCaller(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.TransportFactory()).Wait();
                connection2.Start(host.TransportFactory()).Wait();

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void SendToAllButCallerInGroup(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.TransportFactory()).Wait();
                connection2.Start(host.TransportFactory()).Wait();

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void SendToAll(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.TransportFactory()).Wait();
                connection2.Start(host.TransportFactory()).Wait();

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
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void SendToSelf(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                var connection1 = CreateHubConnection(host);
                var connection2 = CreateHubConnection(host);

                var wh1 = new ManualResetEventSlim(initialState: false);
                var wh2 = new ManualResetEventSlim(initialState: false);

                var hub1 = connection1.CreateHubProxy("SendToSome");
                var hub2 = connection2.CreateHubProxy("SendToSome");

                connection1.Start(host.TransportFactory()).Wait();
                connection2.Start(host.TransportFactory()).Wait();

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
                IHubContext hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext("SendToSome");
                });

                var connection1 = new Client.Hubs.HubConnection("http://foo/");

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
                IHubContext hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext("SendToSome");
                });

                var connection1 = new Client.Hubs.HubConnection("http://foo/");

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
                IHubContext hubContext = null;
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapHubs(configuration);
                    hubContext = configuration.Resolver.Resolve<IConnectionManager>().GetHubContext("SendToSome");
                });

                var connection1 = new Client.Hubs.HubConnection("http://foo/");
                var connection2 = new Client.Hubs.HubConnection("http://foo/");

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

        [Theory]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void JoinAndSendToGroupRenamedHub(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(enableAutoRejoiningGroups: true);

                HubConnection connection = CreateHubConnection(host);
                var wh = new ManualResetEventSlim();

                var hub = connection.CreateHubProxy("groupChat");

                var list = new List<int>();

                hub.On<int>("send", list.Add);

                connection.Start(host.Transport).Wait();

                hub.InvokeWithTimeout("Join", "Foo");

                hub.InvokeWithTimeout("Send", "Foo", "1");

                hub.InvokeWithTimeout("Leave", "Foo");

                for (int i = 0; i < 10; i++)
                {
                    hub.InvokeWithTimeout("Send", "Foo", "2");
                }

                Assert.Equal(1, list.Count);
                Assert.Equal(1, list[0]);

                connection.Stop();
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

        public class User
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public string Room { get; set; }
        }
    }
}
