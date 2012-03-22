using System;
using System.Linq;
using System.Threading;
using Moq;
using SignalR.Client.Transports;
using SignalR.Hosting.Memory;
using Xunit;

namespace SignalR.Client.Tests
{
    public class ConnectionFacts
    {
        public class Start
        {
            [Fact]
            public void FailsIfProtocolVersionIsNull()
            {
                var connection = new Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection)).Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                {
                    ProtocolVersion = null
                }));

                var aggEx = Assert.Throws<AggregateException>(() => connection.Start(transport.Object).Wait());
                var ex = aggEx.GetBaseException();
                Assert.IsType(typeof(InvalidOperationException), ex);
                Assert.Equal("Incompatible protocol version.", ex.Message);
            }
        }

        public class Received
        {
            [Fact]
            public void SendingBigData()
            {
                var host = new MemoryHost();
                host.MapConnection<SampleConnection>("/echo");

                var connection = new Connection("http://foo/echo");

                var wh = new ManualResetEventSlim();
                var n = 0;
                var target = 20;

                connection.Received += data =>
                {
                    n++;
                    if (n == target)
                    {
                        wh.Set();
                    }
                };

                connection.Start(host).Wait();

                var conn = host.ConnectionManager.GetConnection<SampleConnection>();

                for (int i = 0; i < target; ++i)
                {
                    var node = new BigData();
                    conn.Broadcast(node).Wait();
                    Thread.Sleep(1000);
                }

                Assert.True(wh.Wait(TimeSpan.FromMinutes(1)), "Timed out");
            }

            public class BigData
            {
                public string[] Dummy
                {
                    get
                    {
                        return Enumerable.Range(0, 1000).Select(x => new String('*', 500)).ToArray();
                    }
                }
            }

            public class SampleConnection : PersistentConnection
            {
            }
        }
    }
}
