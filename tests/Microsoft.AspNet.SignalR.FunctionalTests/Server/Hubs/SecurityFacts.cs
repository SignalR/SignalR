using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Hubs
{
    public class SecurityFacts
    {
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

                    app.MapConnection<MyConnection>("/echo", config);

                    protectedData = config.Resolver.Resolve<IProtectedData>();
                });

                var connection = new Client.Connection("http://memoryhost/echo");

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
                        var response = await MakeRequest(host, url);
                        reader = new EventSourceStreamReader(response.GetResponseStream());

                        reader.Message = sseEvent =>
                        {
                            if (sseEvent.EventType == EventType.Data &&
                                sseEvent.Data != "initialized")
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

                if (reader != null)
                {
                    reader.Close();
                }

                connection.Stop();
            }
        }

        private string GetUrl(IProtectedData protectedData, Client.Connection connection)
        {
            // Generate a valid token
            string token = protectedData.Protect(Guid.NewGuid().ToString("d"), Purposes.ConnectionId);
            string groupsToken = protectedData.Protect(JsonConvert.SerializeObject(new[] { connection.ConnectionToken }), Purposes.Groups);

            var sb = new StringBuilder("http://memoryhost/echo/");
            sb.Append("?connectionToken=")
              .Append(Uri.EscapeDataString(token))
              .Append("&transport=serverSentEvents")
              .Append("&groupsToken=")
              .Append(Uri.EscapeDataString(groupsToken));

            return sb.ToString();
        }

        private static async Task<Client.Http.IResponse> MakeRequest(MemoryHost host, string url)
        {
            return await host.ProcessRequest(url, r => { }, new Dictionary<string, string>());
        }

        public class MyConnection : PersistentConnection
        {
            protected override Task OnReceived(IRequest request, string connectionId, string data)
            {
                return Connection.Send(connectionId, data.Trim());
            }
        }
    }
}
