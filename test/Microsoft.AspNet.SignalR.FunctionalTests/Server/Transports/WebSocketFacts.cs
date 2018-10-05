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
