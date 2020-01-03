// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Transports
{
    public class WebSocketFacts : HostedTest
    {
        [Theory]
        [InlineData("GetStuff")]
        [InlineData("GetCircular")]
        public async Task ReturningUnserializableObjectsForcesImmediateReconnectWithWebSockets(string method)
        {
            using (var host = CreateHost(HostType.HttpListener, TransportType.Websockets))
            {
                host.Initialize();

                using (var connection = CreateHubConnection(host))
                {
                    var keepAlivesMissed = false;
                    var reconnectedWh = new TaskCompletionSource<object>();

                    connection.ConnectionSlow += () =>
                    {
                        keepAlivesMissed = true;
                    };

                    connection.Reconnected += () => reconnectedWh.TrySetResult(null);

                    var hub = connection.CreateHubProxy("returnsUnserializableObjectHub");

                    await connection.Start(host.Transport);

                    // The return value of GetStuff will cause Json.NET to throw during serialization
                    await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke(method)).OrTimeout();

                    await reconnectedWh.Task.OrTimeout(TimeSpan.FromSeconds(30));
                    Assert.False(keepAlivesMissed);
                }
            }
        }

        // https://github.com/SignalR/SignalR/issues/4412
        [Fact]
        public async Task WebSocketsCanReconnectMultipleTimes()
        {
            using (var host = CreateHost(HostType.HttpListener, TransportType.Websockets))
            {
                var owinHost = (OwinTestHost)host;

                host.Initialize();

                using (var connection = CreateHubConnection(host))
                {
                    var reconnectedWh = new TaskCompletionSource<object>();
                    var reconnectingCount = 0;
                    var reconnectedCount = 0;

                    connection.Reconnecting += () =>
                    {
                        reconnectingCount++;
                    };

                    connection.Reconnected += () =>
                    {
                        reconnectedCount++;
                        reconnectedWh.TrySetResult(null);
                    };

                    var hub = connection.CreateHubProxy("demo");

                    await connection.Start(host.Transport);

                    owinHost.Restart();

                    await reconnectedWh.Task.OrTimeout(TimeSpan.FromSeconds(30));

                    Assert.Equal(1, reconnectingCount);
                    Assert.Equal(1, reconnectedCount);

                    reconnectedWh = new TaskCompletionSource<object>();

                    owinHost.Restart();

                    await reconnectedWh.Task.OrTimeout(TimeSpan.FromSeconds(30));

                    Assert.Equal(2, reconnectingCount);
                    Assert.Equal(2, reconnectedCount);

                    // Give some time for the simultaneous DoReconnect loops described in
                    // https://github.com/SignalR/SignalR/issues/4412#issuecomment-538122907
                    // to rear their ugly heads.
                    await Task.Delay(1000);

                    Assert.Equal(2, reconnectingCount);
                    Assert.Equal(2, reconnectedCount);
                }
            }
        }

        public class ReturnsUnserializableObjectHub : Hub
        {
            public IEnumerable<int> GetStuff()
            {
                yield return 1;
                yield return 2;
                throw new Exception("This is will bork the socket :P");
            }

            public Circular GetCircular()
            {
                return new Circular();
            }

            public class Circular
            {
                public Circular Myself;
                public string Foo;

                public Circular()
                {
                    Myself = this;
                    Foo = "bar";
                }
            }
        }
    }
}
