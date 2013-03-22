using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
            var operation = new TestObservableDbOperation(dbProviderFactory.Object, new[] { new[] { 0, 1 } })
            {
                SupportSqlNotifications = false
            };
            var retryLoopCount = 0;
            var mre = new ManualResetEventSlim(false);
            operation.OnRetyLoopIteration = () =>
            {
                if (++retryLoopCount > 1)
                {
                    mre.Set();
                }
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));

            var result = mre.Wait(TimeSpan.FromMilliseconds(200));
            operation.Dispose();

            // Assert
            Assert.True(result, "Retries weren't completed in expected time");
        }

        [Fact]
        public void UseSqlNotificationsIfAvailable()
        {
            // Arrange
            var dbProviderFactory = new Mock<DbProviderFactory>();
            var mre = new ManualResetEventSlim(false);
            var operation = new TestObservableDbOperation(dbProviderFactory.Object, new[] { new[] { 0, 1 } })
            {
               SupportSqlNotifications = true,
               OnAddSqlDependency = () => mre.Set()
            };
            
            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));

            var result = mre.Wait(TimeSpan.FromMilliseconds(200));
            operation.Dispose();

            // Assert
            Assert.True(result, "SQL notification was not setup in expected time");
        }

        private class TestObservableDbOperation : ObservableDbOperation
        {
            private int[][] _updateLoopRetryDelays = new [] { new [] { 0, 1 } };

            public TestObservableDbOperation(DbProviderFactory dbProviderFactory)
                : base("test-connection-string", "test-command-text", new TraceSource("test"), dbProviderFactory) 
            {

            }

            public TestObservableDbOperation(DbProviderFactory dbProviderFactory, int[][] updateLoopRetryDelays)
                : this(dbProviderFactory)
            {
                _updateLoopRetryDelays = updateLoopRetryDelays;
            }

            public Action OnRetyLoopIteration { get; set; }

            public bool SupportSqlNotifications { get; set; }

            public Action OnAddSqlDependency { get; set; }

            protected override int ExecuteReader(Action<IDataRecord, DbOperation> processRecord, Action<IDbCommand> commandAction)
            {
                if (OnRetyLoopIteration != null)
                {
                    OnRetyLoopIteration();
                }

                if (commandAction != null)
                {
                    commandAction(null);
                }

                return 0;
            }

            protected override bool StartSqlDependencyListener()
            {
                return SupportSqlNotifications;
            }

            protected override int[][] UpdateLoopRetryDelays
            {
                get
                {
                    return _updateLoopRetryDelays;
                }
            }

            protected override void AddSqlDependency(IDbCommand command, Action<SqlNotificationEventArgs> callback)
            {
                if (OnAddSqlDependency != null)
                {
                    OnAddSqlDependency();
                }
            }
        }
    }
}
