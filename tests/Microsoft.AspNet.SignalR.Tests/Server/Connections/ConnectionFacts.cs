using Moq;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client.Tests
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
                var ex = aggEx.Unwrap();
                Assert.IsType(typeof(InvalidOperationException), ex);
                Assert.Equal("Incompatible protocol version.", ex.Message);
            }

            [Fact]
            public void FailedNegotiateShouldNotBeActive()
            {
                var connection = new Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection))
                         .Returns(TaskAsyncHelper.FromError<NegotiationResponse>(new InvalidOperationException("Something failed.")));

                var aggEx = Assert.Throws<AggregateException>(() => connection.Start(transport.Object).Wait());
                var ex = aggEx.Unwrap();
                Assert.IsType(typeof(InvalidOperationException), ex);
                Assert.Equal("Something failed.", ex.Message);
                Assert.Equal(ConnectionState.Disconnected, connection.State);
            }

            [Fact]
            public void FailedStartShouldNotBeActive()
            {
                var connection = new Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection))
                         .Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                         {
                             ProtocolVersion = "1.1",
                             ConnectionId = "Something"
                         }));

                transport.Setup(m => m.Start(connection, null, It.IsAny<CancellationToken>()))
                         .Returns(TaskAsyncHelper.FromError(new InvalidOperationException("Something failed.")));

                var aggEx = Assert.Throws<AggregateException>(() => connection.Start(transport.Object).Wait());
                var ex = aggEx.Unwrap();
                Assert.IsType(typeof(InvalidOperationException), ex);
                Assert.Equal("Something failed.", ex.Message);
                Assert.Equal(ConnectionState.Disconnected, connection.State);
            }
        }
    }
}
