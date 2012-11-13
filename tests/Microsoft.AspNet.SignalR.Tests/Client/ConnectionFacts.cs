using System;
using Microsoft.AspNet.SignalR.Client;
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
