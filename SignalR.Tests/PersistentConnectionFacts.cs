using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Hosting.Memory;
using Xunit;
using System.Threading;
using System;
using System.Linq;

namespace SignalR.Tests
{
    public class PersistentConnectionFacts
    {
        public class OnConnectedAsync
        {
            [Fact]
            public void GroupsAreNotReadOnConnectedAsync()
            {
                var host = new MemoryHost();
                host.MapConnection<MyConnection>("/echo");

                var connection = new Client.Connection("http://foo/echo");
                connection.Groups = new List<string> { typeof(MyConnection).FullName + ".test" };
                connection.Received += data =>
                {
                    Assert.False(true, "Unexpectedly received data");
                };

                connection.Start(host).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }

            [Fact]
            public void GroupsAreNotReadOnConnectedAsyncLongPolling()
            {
                var host = new MemoryHost();
                host.MapConnection<MyConnection>("/echo");

                var connection = new Client.Connection("http://foo/echo");
                connection.Groups = new List<string> { typeof(MyConnection).FullName + ".test" };
                connection.Received += data =>
                {
                    Assert.False(true, "Unexpectedly received data");
                };

                var transport = new Client.Transports.LongPollingTransport(host);
                connection.Start(transport).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }
    }

    public class MyConnection : PersistentConnection
    {
        protected override Task OnConnectedAsync(Hosting.IRequest request, string connectionId)
        {
            return SendToGroup("test", "hey");
        }
    }
}
