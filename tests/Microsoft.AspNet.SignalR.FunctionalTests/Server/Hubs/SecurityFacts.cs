using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents;
using Microsoft.AspNet.SignalR.Hosting.Memory;
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
                host.Configure(app =>
                {
                    app.MapConnection<MyConnection>("/echo");
                });

                var connection = new Client.Connection("http://memoryhost/echo");

                var connectionTcs = new TaskCompletionSource<string>();
                var spyTcs = new TaskCompletionSource<string>();

                connection.Received += data =>
                {
                    connectionTcs.SetResult(data.Trim());
                };

                connection.Start(host).Wait();

                var wh = new ManualResetEventSlim();
                EventSourceStreamReader reader = null;

                Task.Run(async () =>
                {
                    string url = GetUrl(connection);
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
                    wh.Set();
                });

                wh.Wait();

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

        private string GetUrl(Client.Connection connection)
        {
            string id = Guid.NewGuid().ToString("d");

            var sb = new StringBuilder("http://memoryhost/echo/");
            sb.Append("?connectionId=")
              .Append(Uri.EscapeDataString(id))
              .Append("&transport=serverSentEvents")
              .Append("&groups=")
              .Append(Uri.EscapeDataString(JsonConvert.SerializeObject(new[] { connection.ConnectionId })));

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

            protected override IList<string> OnRejoiningGroups(IRequest request, IList<string> groups, string connectionId)
            {
                return groups;
            }
        }
    }
}
