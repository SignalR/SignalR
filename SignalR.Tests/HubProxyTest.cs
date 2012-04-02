using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using SignalR.Client.Hubs;
using Xunit;

namespace SignalR.Tests
{
    public class HubProxyTest
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
                State = new Dictionary<string, object>
                {
                    { "state", 1 }
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

            hubProxy.InvokeEvent("foo", new object[] { });
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

            hubProxy.InvokeEvent("foo", new object[] { 1 });
            Assert.True(eventRaised);
        }

        [Fact]
        public void GetSubscriptionsReturnsListOfSubscriptions()
        {
            var connection = new Mock<SignalR.Client.IConnection>();
            var hubProxy = new HubProxy(connection.Object, "foo");

            hubProxy.On<int>("foo", i => { });

            hubProxy.On("baz", () => { });

            var subscriptions = hubProxy.GetSubscriptions().ToList();
            Assert.Equal(2, subscriptions.Count);
            Assert.Equal("foo", subscriptions[0]);
            Assert.Equal("baz", subscriptions[1]);
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
    }
}
