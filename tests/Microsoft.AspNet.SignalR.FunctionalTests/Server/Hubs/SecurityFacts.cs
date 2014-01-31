using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common;
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
                    var inGroup = new AsyncManualResetEvent();

                    connection.Received += data =>
                    {
                        if (data == "group")
                        {
                            inGroup.Set();
                        }
                    };

                    await connection.Start(host);

                    await inGroup.WaitAsync(TimeSpan.FromSeconds(10));

                    Assert.NotNull(connection.GroupsToken);

                    var spyWh = new AsyncManualResetEvent();
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
                            spyWh.Set();
                        }
                    };

                    reader.Start();
                    await connection.Send("random");

                    Assert.False(await spyWh.WaitAsync(TimeSpan.FromSeconds(5)));
                }
            }
        }

        [Fact]
        public void ConnectionIdsCantBeUsedAsGroups()
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
                        connectionTcs.SetResult(data.Trim());
                    };

                    connection.Start(host).Wait();

                    var tcs = new TaskCompletionSource<object>();
                    EventSourceStreamReader reader = null;

                    Task.Run(async () =>
                    {
                        try
                        {
                            string url = GetUrl(protectedData, connection);
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
                            tcs.TrySetResult(null);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    });

                    tcs.Task.Wait();

                    connection.SendWithTimeout("STUFFF");

                    Assert.True(connectionTcs.Task.Wait(TimeSpan.FromSeconds(5)));
                    Assert.Equal("STUFFF", connectionTcs.Task.Result);
                    Assert.False(spyTcs.Task.Wait(TimeSpan.FromSeconds(5)));
                }
            }
        }

        private string GetUrl(IProtectedData protectedData, Client.Connection connection)
        {
            // Generate a valid token
            string groupsToken = protectedData.Protect(JsonConvert.SerializeObject(new[] { connection.ConnectionToken }), Purposes.Groups);

            return GetUrl(protectedData, connection, groupsToken);
        }

        private string GetUrl(IProtectedData protectedData, Client.Connection connection, string groupsToken)
        {
            // Generate a valid token
            string connectionToken = protectedData.Protect(Guid.NewGuid().ToString("d") + ':', Purposes.ConnectionToken);

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
