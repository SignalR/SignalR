using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNet.SignalR.SqlServer;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.SqlServer
{
    public class ObservableSqlOperationFacts
    {
        [Fact]
        public void DoesntUseSqlNotificationsIfUnavailable()
        {
            // Arrange
            var dbProviderFactory = new Mock<DbProviderFactory>();
            var operation = new TestObservableDbOperation(dbProviderFactory.Object);
            var recordCount = 0;

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => {
                if (++recordCount == 2)
                {

                };
            }));

            // Assert

        }

        private class TestObservableDbOperation : ObservableDbOperation
        {
            public TestObservableDbOperation(DbProviderFactory dbProviderFactory)
                : base("test-connection-string", "test-command-text", new TraceSource("test"), dbProviderFactory) 
            {

            }

            protected override bool StartSqlDependencyListener()
            {
                return false;
            }

            protected override int[][] UpdateLoopRetryDelays
            {
                get
                {
                    return new[] { new[] { 10, 1 } };
                }
            }
        }
    }
}
