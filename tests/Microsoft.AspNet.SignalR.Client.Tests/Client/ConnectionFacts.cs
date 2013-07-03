using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Tests
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

        [Fact]
        public void ConnectionTraceIsProperlyFiltered()
        {
            var traceNumber = 0;
            var traceOutput = new StringBuilder();
            var traceWriter = new StringWriter(traceOutput);
            var connection = new Client.Connection("http://test");
            var timestampPattern = new Regex(@"^.* - ", RegexOptions.Multiline);

            Func<string> traceWithoutTimestamps = () =>
                timestampPattern.Replace(traceOutput.ToString(), String.Empty);

            Action traceAllLevels = () =>
            {
                connection.Trace(TraceLevels.Messages, "{0}: Message", ++traceNumber);
                connection.Trace(TraceLevels.Events, "{0}: Event", ++traceNumber);
                connection.Trace(TraceLevels.StateChanges, "{0}: State Change", ++traceNumber);
            };

            connection.TraceWriter = traceWriter;

            traceAllLevels();
            Assert.Equal("1: Message\r\n2: Event\r\n3: State Change\r\n", traceWithoutTimestamps());
            traceOutput.Clear();

            connection.TraceLevel = TraceLevels.All;
            traceAllLevels();
            Assert.Equal("4: Message\r\n5: Event\r\n6: State Change\r\n", traceWithoutTimestamps());
            traceOutput.Clear();

            connection.TraceLevel = TraceLevels.Messages;
            traceAllLevels();
            Assert.Equal("7: Message\r\n", traceWithoutTimestamps());
            traceOutput.Clear();

            connection.TraceLevel = TraceLevels.Events;
            traceAllLevels();
            Assert.Equal("11: Event\r\n", traceWithoutTimestamps());
            traceOutput.Clear();

            connection.TraceLevel = TraceLevels.StateChanges;
            traceAllLevels();
            Assert.Equal("15: State Change\r\n", traceWithoutTimestamps());
            traceOutput.Clear();

            connection.TraceLevel = TraceLevels.None;
            traceAllLevels();
            Assert.Equal(String.Empty, traceWithoutTimestamps());
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

            [Fact]
            public void AsyncStartShouldBeConnected()
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
                         .Returns(TaskAsyncHelper.Delay(TimeSpan.FromMilliseconds(100)));

                Assert.True(connection.Start(transport.Object).Wait(TimeSpan.FromSeconds(5)), "Start hung.");
                Assert.Equal(ConnectionState.Connected, connection.State);
            }

            [Fact]
            public void AsyncStartShouldFailIfTransportStartFails()
            {
                var connection = new Client.Connection("http://test");
                var transport = new Mock<IClientTransport>();
                var ex = new Exception();
                transport.Setup(m => m.Negotiate(connection))
                         .Returns(TaskAsyncHelper.FromResult(new NegotiationResponse
                         {
                             ProtocolVersion = "1.2",
                             ConnectionId = "Something"
                         }));

                transport.Setup(m => m.Start(connection, null, It.IsAny<CancellationToken>()))
                         .Returns(TaskAsyncHelper.Delay(TimeSpan.FromMilliseconds(100)).Then(() =>
                         {
                             throw ex;
                         }));

                var aggEx = Assert.Throws<AggregateException>(() => connection.Start(transport.Object).Wait());

                Assert.Equal(aggEx.Unwrap(), ex);
                Assert.Equal(ConnectionState.Disconnected, connection.State);
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
