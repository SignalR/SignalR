using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;
using SignalR.Hubs;
using Xunit;

namespace SignalR.Tests
{
    public class HubProxyFacts : IDisposable
    {
        [Fact]
        public void InvokeWithErrorInHubResultReturnsFaultedTask()
        {
            var result = new HubResult<object>
            {
                Error = "This in an error"
            };

            var connection = new Mock<SignalR.Client.IConnection>();
            connection.Setup(m => m.Send<HubResult<object>>(It.IsAny<string>()))
                      .Returns(TaskAsyncHelper.FromResult(result));

            var hubProxy = new HubProxy(connection.Object, "foo");

            AssertAggregateException(() => hubProxy.Invoke("Invoke").Wait(),
                                     "This in an error");
        }

        [Fact]
        public void InvokeWithStateCopiesStateToHubProxy()
        {
            var result = new HubResult<object>
            {
                State = new Dictionary<string, JToken>
                {
                    { "state", JToken.FromObject(1) }
                }
            };

            var connection = new Mock<SignalR.Client.IConnection>();
            connection.Setup(m => m.Send<HubResult<object>>(It.IsAny<string>()))
                      .Returns(TaskAsyncHelper.FromResult(result));

            var hubProxy = new HubProxy(connection.Object, "foo");

            hubProxy.Invoke("Anything").Wait();

            Assert.Equal(1, hubProxy["state"]);
        }

        [Fact]
        public void InvokeReturnsHubsResult()
        {
            var hubResult = new HubResult<object>
            {
                Result = "Something"
            };

            var connection = new Mock<SignalR.Client.IConnection>();
            connection.Setup(m => m.Send<HubResult<object>>(It.IsAny<string>()))
                      .Returns(TaskAsyncHelper.FromResult(hubResult));

            var hubProxy = new HubProxy(connection.Object, "foo");

            var result = hubProxy.Invoke<object>("Anything").Result;

            Assert.Equal(result, "Something");
        }

        [Fact]
        public void InvokeEventRaisesEvent()
        {
            var connection = new Mock<SignalR.Client.IConnection>();
            var hubProxy = new HubProxy(connection.Object, "foo");
            bool eventRaised = false;

            hubProxy.On("foo", () =>
            {
                eventRaised = true;
            });

            hubProxy.InvokeEvent("foo", new JToken[] { });
            Assert.True(eventRaised);
        }

        [Fact]
        public void InvokeEventRaisesEventWithData()
        {
            var connection = new Mock<SignalR.Client.IConnection>();
            var hubProxy = new HubProxy(connection.Object, "foo");
            bool eventRaised = false;

            hubProxy.On<int>("foo", (i) =>
            {
                eventRaised = true;
                Assert.Equal(1, i);
            });

            hubProxy.InvokeEvent("foo", new[] { JToken.FromObject(1) });
            Assert.True(eventRaised);
        }

        [Fact]
        public void EndToEndTest()
        {
            var host = new MemoryHost();
            host.MapHubs();

            var hubConnection = new HubConnection("http://fake");
            IHubProxy proxy = hubConnection.CreateProxy("ChatHub");
            var wh = new ManualResetEvent(false);

            proxy.On("addMessage", data =>
            {
                Assert.Equal("hello", data);
                wh.Set();
            });

            hubConnection.Start(host).Wait();

            proxy.Invoke("Send", "hello").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void HubNamesAreNotCaseSensitive()
        {
            var host = new MemoryHost();
            host.MapHubs();

            var hubConnection = new HubConnection("http://fake");
            IHubProxy proxy = hubConnection.CreateProxy("chatHub");
            var wh = new ManualResetEvent(false);

            proxy.On("addMessage", data =>
            {
                Assert.Equal("hello", data);
                wh.Set();
            });

            hubConnection.Start(host).Wait();

            proxy.Invoke("Send", "hello").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void UnableToCreateHubThrowsError()
        {
            var host = new MemoryHost();
            host.MapHubs();

            var hubConnection = new HubConnection("http://fake");
            IHubProxy proxy = hubConnection.CreateProxy("MyHub2");

            hubConnection.Start(host).Wait();
            Assert.Throws<MissingMethodException>(() => proxy.Invoke("Send", "hello").Wait());
        }

        private void AssertAggregateException(Action action, string message)
        {
            try
            {
                action();
            }
            catch (AggregateException ex)
            {
                Assert.Equal(ex.Unwrap().Message, message);
            }
        }

        public class MyHub2 : Hub
        {
            public MyHub2(int n)
            {

            }

            public void Send(string value)
            {

            }
        }

        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public class ChatHub : Hub
        {
            public Task Send(string message)
            {
                return Clients.All.addMessage(message);
            }
        }
    }
}
