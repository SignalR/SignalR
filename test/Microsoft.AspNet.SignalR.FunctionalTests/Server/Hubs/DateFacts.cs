// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common.Hubs;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server
{
    public class DateFacts : HostedTest
    {
        [Fact]
        public async Task DateAsStringRoundtrip()
        {
            var serializer = new JsonSerializer();
            serializer.DateParseHandling = DateParseHandling.None;
            string expected = "1960-08-25T00:00:00";
            TypeWithDateAsString callback = null;

            using (var host = CreateHost(HostType.Memory, TransportType.Auto))
            {
                host.Initialize();
                host.Resolver.Register(typeof(JsonSerializer), () => serializer);

                var connection = CreateHubConnection(host);
                connection.JsonSerializer.DateParseHandling = DateParseHandling.None;

                var hub = connection.CreateHubProxy("DateAsStringHub");
                var request = new TypeWithDateAsString();
                request.DateAsString = expected;
                var wh = new TaskCompletionSource<object>();
                hub.On<TypeWithDateAsString>("Callback", (data) =>
                {
                    callback = data;
                    wh.TrySetResult(null);
                });

                await connection.Start(host.Transport);
                var response = await hub.Invoke<TypeWithDateAsString>("Invoke", request);
                Assert.Equal(expected, response.DateAsString);
                await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                Assert.Equal(expected, callback.DateAsString);
            }
        }
    }
}
