using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Utilities;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class TypedHubProxyFacts
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
            var hubProxy = new TypedHubProxy<IServerHub, IClient>(connection.Object, "foo");

            TestUtilities.AssertAggregateException(() => hubProxy.Invoke(hub => hub.Invoke()).Wait(),
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

            var hubProxy = new TypedHubProxy<IServerHub, IClient>(connection.Object, "foo");

            hubProxy.Invoke(hub => hub.Anything()).Wait();

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

            var hubProxy = new TypedHubProxy<IServerHub, IClient>(connection.Object, "foo");

            var result = hubProxy.Invoke<object>(hub => hub.Anything()).Result;

            Assert.Equal(result, "Something");
        }

        [Fact]
        public void InvokeEventRaisesEvent()
        {
            var connection = new Mock<IHubConnection>();
            connection.SetupGet(x => x.JsonSerializer).Returns(new JsonSerializer());

            var hubProxy = new TypedHubProxy<IServerHub, IClient>(connection.Object, "foo");
            bool eventRaised = false;

            hubProxy.On(hub => hub.Foo, () =>
            {
                eventRaised = true;
            });

            hubProxy.InvokeEvent(hub => hub.Foo());
            Assert.True(eventRaised);
        }

        [Fact]
        public void InvokeEventRaisesEventWithData()
        {
            var connection = new Mock<IHubConnection>();
            connection.SetupGet(x => x.JsonSerializer).Returns(new JsonSerializer());

            var hubProxy = new TypedHubProxy<IServerHub, IClient>(connection.Object, "foo");
            bool eventRaised = false;

            hubProxy.On<int>(hub => hub.Foo, (arg) =>
            {
                eventRaised = true;
                Assert.Equal(1, arg);
            });

            hubProxy.InvokeEvent(hub => hub.Foo(1));
            Assert.True(eventRaised);
        }

        [Fact]
        public void HubCallbackClearedOnFailedInvocation()
        {
            var connection = new Mock<HubConnection>("http://foo");
            var tcs = new TaskCompletionSource<object>();

            tcs.TrySetCanceled();

            connection.Setup(c => c.Send(It.IsAny<string>())).Returns(tcs.Task);

            var hubProxy = new TypedHubProxy<IServerHub, IClient>(connection.Object, "foo");

            var aggEx = Assert.Throws<AggregateException>(() => { hubProxy.Invoke(hub => hub.Foo("arg1")).Wait(); });
            var ex = aggEx.Unwrap();

            Assert.IsType(typeof(TaskCanceledException), ex);

            Assert.Equal(connection.Object._callbacks.Count, 0);
        }

        [Fact(Timeout = 5000)]
        public void FailedHubCallbackDueToReconnectFollowedByInvoke()
        {
            // Arrange
            var testTcs = new TaskCompletionSource<object>();
            var crashTcs = new TaskCompletionSource<object>();
            var connection = new HubConnection("http://test");
            var transport = new Mock<IClientTransport>();

            transport.Setup(t => t.Negotiate(connection, /* connectionData: */ It.IsAny<string>()))
                     .Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                     {
                         ProtocolVersion = connection.Protocol.ToString(),
                         ConnectionId = "Something",
                         DisconnectTimeout = 120
                     }));

            transport.Setup(t => t.Start(connection, /* connectionData: */ It.IsAny<string>(), /* disconnectToken: */ It.IsAny<CancellationToken>()))
                     .Returns(TaskAsyncHelper.Empty);

            transport.Setup(t => t.Send(connection, /* data: */ It.Is<string>(s => s.IndexOf("Crash") >= 0), /* connectionData: */ It.IsAny<string>()))
                     .Returns(crashTcs.Task) // We want this task to never complete as the call to EnsureReconnecting will do it for us
                     .Callback(() =>
                     {
                         Task.Run(() =>
                         {
                             try
                             {
                                 // EnsureReconnecting will change the state and ultimately clear the pending invocation callbacks
                                 connection.EnsureReconnecting();
                                 testTcs.SetResult(null);
                             }
                             catch (Exception ex)
                             {
                                 testTcs.SetException(ex);
                             }
                         });
                     });

            var proxy = new TypedHubProxy<IServerHub, IClient>(connection, "test");

            // Act
            connection.Start(transport.Object).Wait();
            var crashTask = proxy.Invoke(hub => hub.Crash())
                .ContinueWith(t => proxy.Invoke(hub => hub.Test()),
                    TaskContinuationOptions.ExecuteSynchronously); // We need to ensure this executes sync so we're on the same stack

            // Assert
            Assert.Throws(typeof(AggregateException), () => crashTask.Wait());
            Assert.DoesNotThrow(() => testTcs.Task.Wait());
        }

        private interface IServerHub
        {
            void Invoke();
            object Anything();

            void Foo(string arg);

            void Crash();
            void Test();

        }

        private interface IClient
        {
            void Foo();
            void Foo(int arg);
        }
    }
}