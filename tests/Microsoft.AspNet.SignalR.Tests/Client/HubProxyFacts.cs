﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;
using Microsoft.AspNet.SignalR.Tests.Utilities;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubProxyFacts
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

            TestUtilities.AssertAggregateException(() => hubProxy.Invoke("Invoke").Wait(),
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
    }
}