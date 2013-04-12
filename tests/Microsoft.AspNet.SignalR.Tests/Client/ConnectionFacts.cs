using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ConnectionFacts : IDisposable
    {
        [Fact]
        public void ConnectionStateChangedEventIsCalledWithAppropriateArguments()
        {
            var connection = new Client.Connection("http://test");

            connection.StateChanged += stateChange =>
            {
                Assert.Equal(ConnectionState.Disconnected, stateChange.OldState);
                Assert.Equal(ConnectionState.Connecting, stateChange.NewState);
            };

            Assert.True(((Client.IConnection)connection).ChangeState(ConnectionState.Disconnected, ConnectionState.Connecting));
        }

        [Fact]
        public void VerifyThatChangingTheJsonSerializerWorks()
        {
            var connection = new Client.Connection("http://test");

            var firstInstance = connection.JsonSerializer;

            connection.JsonSerializer = new Newtonsoft.Json.JsonSerializer
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
            };

            var secondInstance = connection.JsonSerializer;

            Assert.NotSame(firstInstance, secondInstance);
        }

        public class Start
        {
            [Fact]
            public void FailsIfProtocolVersionIsNull()
            {
                var connection = new Client.Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection)).Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                {
                    ProtocolVersion = null
                }));

                var aggEx = Assert.Throws<AggregateException>(() => connection.Start(transport.Object).Wait());
                var ex = aggEx.Unwrap();
                Assert.IsType(typeof(InvalidOperationException), ex);
                Assert.Equal("You are using a version of the client that isn't compatible with the server. Client version 1.2, server version null.", ex.Message);
            }

            [Fact]
            public void FailedNegotiateShouldBeDisconnected()
            {
                var connection = new Client.Connection("http://test");
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
            public void CancelledNegotiateShouldBeDisconnected()
            {
                var connection = new Client.Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection))
                         .Returns(() =>
                         {
                             var tcs = new TaskCompletionSource<NegotiationResponse>();
                             tcs.SetCanceled();
                             return tcs.Task;
                         });

                var aggEx = Assert.Throws<AggregateException>(() => connection.Start(transport.Object).Wait());
                var ex = aggEx.Unwrap();
                Assert.IsType(typeof(TaskCanceledException), ex);
                Assert.Equal(ConnectionState.Disconnected, connection.State);
            }

            [Fact]
            public void FailedStartShouldBeDisconnected()
            {
                var connection = new Client.Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection))
                         .Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                         {
                             ProtocolVersion = "1.2",
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

            [Fact]
            public void CancelledStartShouldBeDisconnected()
            {
                var connection = new Client.Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection))
                         .Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                         {
                             ProtocolVersion = "1.2",
                             ConnectionId = "Something"
                         }));

                transport.Setup(m => m.Start(connection, null, It.IsAny<CancellationToken>()))
                         .Returns(() =>
                         {
                             var tcs = new TaskCompletionSource<object>();
                             tcs.SetCanceled();
                             return tcs.Task;
                         });

                var aggEx = Assert.Throws<AggregateException>(() => connection.Start(transport.Object).Wait());
                var ex = aggEx.Unwrap();
                Assert.IsType(typeof(TaskCanceledException), ex);
                Assert.Equal(ConnectionState.Disconnected, connection.State);
            }

            [Fact]
            public void StartShouldBeConnected()
            {
                var connection = new Client.Connection("http://test");
                var transport = new Mock<IClientTransport>();
                transport.Setup(m => m.Negotiate(connection))
                         .Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                         {
                             ProtocolVersion = "1.2",
                             ConnectionId = "Something"
                         }));

                transport.Setup(m => m.Start(connection, null, It.IsAny<CancellationToken>()))
                         .Returns(TaskAsyncHelper.Empty);

                connection.Start(transport.Object).Wait();
                Assert.Equal(ConnectionState.Connected, connection.State);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
