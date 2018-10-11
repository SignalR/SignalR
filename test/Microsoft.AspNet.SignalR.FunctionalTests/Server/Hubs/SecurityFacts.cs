// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Newtonsoft.Json;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Hubs
{
    public class SecurityFacts
    {
        [Fact]
        public async Task GroupsTokenIsPerConnectionId()
        {
            using (var host = new MemoryHost())
            {
                IProtectedData protectedData = null;

                host.Configure(app =>
                {
                    var config = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR<MyGroupConnection>("/echo", config);

                    protectedData = config.Resolver.Resolve<IProtectedData>();
                });

                var connection = new Client.Connection("http://memoryhost/echo");

                using (connection)
                {
                    var inGroup = new TaskCompletionSource<object>();

                    connection.Received += data =>
                    {
                        if (data == "group")
                        {
                            inGroup.TrySetResult(null);
                        }
                    };

                    await connection.Start(host);

                    await inGroup.Task.OrTimeout(TimeSpan.FromSeconds(10));

                    Assert.NotNull(connection.GroupsToken);

                    var spyWh = new TaskCompletionSource<object>();
                    var hackerConnection = new Client.Connection(connection.Url)
                    {
                        ConnectionId = "hacker"
                    };

                    var url = GetUrl(protectedData, connection, connection.GroupsToken);
                    var response = await host.Get(url, r => { }, isLongRunning: true);
                    var reader = new EventSourceStreamReader(hackerConnection, response.GetStream());

                    reader.Message = sseEvent =>
                    {
                        if (sseEvent.EventType == EventType.Data &&
                            sseEvent.Data != "initialized" &&
                            sseEvent.Data != "{}")
                        {
                            spyWh.TrySetResult(null);
                        }
                    };

                    reader.Start();
                    await connection.Send("random");

                    await Task.Delay(TimeSpan.FromSeconds(5));
                    Assert.False(spyWh.Task.IsCompleted);
                }
            }
        }

        [Fact]
        public async Task ConnectionIdsCantBeUsedAsGroups()
        {
            using (var host = new MemoryHost())
            {
                IProtectedData protectedData = null;

                host.Configure(app =>
                {
                    var config = new ConnectionConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    app.MapSignalR<MyConnection>("/echo", config);

                    protectedData = config.Resolver.Resolve<IProtectedData>();
                });

                var connection = new Client.Connection("http://memoryhost/echo");

                using (connection)
                {
                    var connectionTcs = new TaskCompletionSource<string>();
                    var spyTcs = new TaskCompletionSource<string>();

                    connection.Received += data =>
                    {
                        connectionTcs.TrySetResult(data.Trim());
                    };

                    await connection.Start(host).OrTimeout();

                    EventSourceStreamReader reader = null;

                    await Task.Run(async () =>
                    {
                        var url = GetUrl(protectedData, connection);
                        var response = await host.Get(url, r => { }, isLongRunning: true);
                        reader = new EventSourceStreamReader(connection, response.GetStream());

                        reader.Message = sseEvent =>
                        {
                            if (sseEvent.EventType == EventType.Data &&
                                sseEvent.Data != "initialized" &&
                                sseEvent.Data != "{}")
                            {
                                spyTcs.TrySetResult(sseEvent.Data);
                            }
                        };

                        reader.Start();
                    });

                    await connection.Send("STUFFF").OrTimeout();

                    Assert.Equal("STUFFF", await connectionTcs.Task.OrTimeout());
                }
            }
        }

        private string GetUrl(IProtectedData protectedData, Client.Connection connection)
        {
            // Generate a valid token
            var groupsToken = protectedData.Protect(JsonConvert.SerializeObject(new[] { connection.ConnectionToken }), Purposes.Groups);

            return GetUrl(protectedData, connection, groupsToken);
        }

        private string GetUrl(IProtectedData protectedData, Client.Connection connection, string groupsToken)
        {
            // Generate a valid token
            var connectionToken = protectedData.Protect(Guid.NewGuid().ToString("d") + ':', Purposes.ConnectionToken);

            return GetUrl(protectedData, connection, connectionToken, groupsToken);
        }

        private string GetUrl(IProtectedData protectedData, Client.Connection connection, string connectionToken, string groupsToken)
        {
            var sb = new StringBuilder(connection.Url);
            sb.Append("?connectionToken=")
              .Append(Uri.EscapeDataString(connectionToken))
              .Append("&transport=serverSentEvents")
              .Append("&groupsToken=")
              .Append(Uri.EscapeDataString(groupsToken));

            return sb.ToString();
        }

        public class MyConnection : PersistentConnection
        {
            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Send(connectionId, data.Trim());
            }
        }

        public class MyGroupConnection : PersistentConnection
        {
            protected override Task OnConnected(IRequest request, string connectionId)
            {
                Groups.Add(connectionId, "group").ContinueWith(task =>
                {
                    Connection.Send(connectionId, "group");
                });

                return base.OnConnected(request, connectionId);
            }

            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Groups.Send("group", data);
            }
        }
    }
}
