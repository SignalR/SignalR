using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Tests.Utilities;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class HubProxyFacts
    {
        [Fact]
        public void InvokeWithErrorInHubResultReturnsFaultedTask()
        {
            var hubResult = new HubResult
            {
                Error = "This in an error"
            };

            var connection = new Mock<IHubConnection>();
            connection.Setup(m => m.RegisterCallback(It.IsAny<Action<HubResult>>()))
                      .Callback<Action<HubResult>>(callback =>
                      {
                          callback(hubResult);
                      });

            connection.Setup(m => m.Send(It.IsAny<string>()))
                      .Returns(TaskAsyncHelper.Empty);

            connection.SetupGet(x => x.JsonSerializer).Returns(new JsonSerializer());

            var hubProxy = new HubProxy(connection.Object, "foo");

            TestUtilities.AssertAggregateException(() => hubProxy.Invoke("Invoke").Wait(),
                                     "This in an error");
        }

        [Fact]
        public void InvokeWithStateCopiesStateToHubProxy()
        {
            var hubResult = new HubResult
            {
                State = new Dictionary<string, JToken>
                {
                    { "state", JToken.FromObject(1) }
                }
            };

            var connection = new Mock<IHubConnection>();
            connection.Setup(m => m.RegisterCallback(It.IsAny<Action<HubResult>>()))
                      .Callback<Action<HubResult>>(callback =>
                      {
                          callback(hubResult);
                      });

            connection.Setup(m => m.Send(It.IsAny<string>()))
                      .Returns(TaskAsyncHelper.Empty);

            connection.SetupGet(x => x.JsonSerializer).Returns(new JsonSerializer());

            var hubProxy = new HubProxy(connection.Object, "foo");

            hubProxy.Invoke("Anything").Wait();

            Assert.Equal(1, hubProxy["state"]);
        }

        [Fact]
        public void InvokeReturnsHubsResult()
        {
            var hubResult = new HubResult
            {
                Result = "Something"
            };

            var connection = new Mock<IHubConnection>();
            connection.Setup(m => m.RegisterCallback(It.IsAny<Action<HubResult>>()))
                      .Callback<Action<HubResult>>(callback =>
                      {
                          callback(hubResult);
                      });

            connection.Setup(m => m.Send(It.IsAny<string>()))
                      .Returns(TaskAsyncHelper.Empty);

            connection.SetupGet(x => x.JsonSerializer).Returns(new JsonSerializer());

            var hubProxy = new HubProxy(connection.Object, "foo");

            var result = hubProxy.Invoke<object>("Anything").Result;

            Assert.Equal(result, "Something");
        }

        [Fact]
        public void InvokeEventRaisesEvent()
        {
            var connection = new Mock<IHubConnection>();
            connection.SetupGet(x => x.JsonSerializer).Returns(new JsonSerializer());

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
            var connection = new Mock<IHubConnection>();
            connection.SetupGet(x => x.JsonSerializer).Returns(new JsonSerializer());

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
        public void HubCallbacksClearedOnDisconnect()
        {
            var connection = new HubConnection("http://foo");

            ((IHubConnection)connection).RegisterCallback(result => { });

            connection.Start();

            ((IHubConnection)connection).Disconnect();

            Assert.Equal(connection._callbacks.Count, 0);
        }

        [Fact]
        public void HubCallbacksClearedOnReconnect()
        {
            var connection = new HubConnection("http://foo");

            ((IHubConnection)connection).RegisterCallback(result => { });

            connection.Start();

            ((IConnection)connection).OnReconnecting();

            Assert.Equal(connection._callbacks.Count, 0);
        }

        [Fact]
        public void HubCallbackClearedOnFailedInvocation()
        {
            var connection = new Mock<HubConnection>("http://foo");
            var tcs = new TaskCompletionSource<object>();

            tcs.TrySetCanceled();

            connection.Setup(c => c.Send(It.IsAny<string>())).Returns(tcs.Task);

            var hubProxy = new HubProxy(connection.Object, "foo");

            var aggEx = Assert.Throws<AggregateException>(() => { hubProxy.Invoke("foo", "arg1").Wait(); });
            var ex = aggEx.Unwrap();

            Assert.IsType(typeof(TaskCanceledException), ex);
                       
            Assert.Equal(connection.Object._callbacks.Count, 0);
        }
    }
}