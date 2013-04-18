using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR.SqlServer;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.SqlServer
{
    public class ObservableSqlOperationFacts
    {
        private static readonly List<Tuple<int, int>> _defaultRetryDelays = new List<Tuple<int, int>> { new Tuple<int, int>(0, 1) };

        [Theory(Timeout = 1500)]
        [InlineData(true)]
        [InlineData(false)]
        public void UseSqlNotificationsIfAvailable(bool supportSqlNotifications)
        {
            // Arrange
            var sqlDependencyAdded = false;
            var retryLoopCount = 0;
            var mre = new ManualResetEventSlim();
            var dbProviderFactory = new MockDbProviderFactory();
            var dbBehavior = new Mock<IDbBehavior>();
            dbBehavior.Setup(db => db.UpdateLoopRetryDelays).Returns(_defaultRetryDelays);
            dbBehavior.Setup(db => db.StartSqlDependencyListener()).Returns(supportSqlNotifications);
            dbBehavior.Setup(db => db.AddSqlDependency(It.IsAny<IDbCommand>(), It.IsAny<Action<SqlNotificationEventArgs>>()))
                .Callback(() =>
                {
                    sqlDependencyAdded = true;
                    mre.Set();
                });
            var operation = new ObservableDbOperation("test", "test", new TraceSource("test"), dbProviderFactory, dbBehavior.Object);
            operation.Faulted += _ => mre.Set();
            operation.Queried += () =>
            {
                retryLoopCount++;
                if (retryLoopCount > 1)
                {
                    mre.Set();
                }
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));
            mre.Wait();
            operation.Dispose();

            // Assert
            Assert.Equal(supportSqlNotifications, sqlDependencyAdded);
        }

        [Theory(Timeout = 1500)]
        [InlineData(1, null, null)]
        [InlineData(5, null, null)]
        [InlineData(10, null, null)]
        [InlineData(1, 5, 10)]
        public void DoesRetryLoopConfiguredNumberOfTimes(int? length1, int? length2, int? length3)
        {
            // Arrange
            var retryLoopCount = 0;
            var mre = new ManualResetEventSlim();
            var retryLoopArgs = new List<int?>(new[] { length1, length2, length3 }).Where(l => l.HasValue);
            var retryLoopTotal = retryLoopArgs.Sum().Value;
            var retryLoopDelays = new List<Tuple<int, int>>(retryLoopArgs.Select(l => new Tuple<int, int>(0, l.Value)));
            var sqlDependencyCreated = false;
            var dbProviderFactory = new MockDbProviderFactory();
            var dbBehavior = new Mock<IDbBehavior>();
            dbBehavior.Setup(db => db.UpdateLoopRetryDelays).Returns(retryLoopDelays);
            dbBehavior.Setup(db => db.StartSqlDependencyListener()).Returns(true);
            dbBehavior.Setup(db => db.AddSqlDependency(It.IsAny<IDbCommand>(), It.IsAny<Action<SqlNotificationEventArgs>>()))
                .Callback(() =>
                {
                    sqlDependencyCreated = true;
                    mre.Set();
                });
            var operation = new ObservableDbOperation("test", "test", new TraceSource("test"), dbProviderFactory, dbBehavior.Object);
            operation.Faulted += _ => mre.Set();
            operation.Queried += () =>
            {
                if (!sqlDependencyCreated)
                {
                    // Only update the loop count if the SQL dependency hasn't been created yet (we're still in the loop)
                    retryLoopCount++;
                }
                if (retryLoopCount == retryLoopTotal)
                {
                    mre.Set();
                }
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));
            mre.Wait();
            operation.Dispose();

            // Assert
            Assert.Equal(retryLoopTotal, retryLoopCount);
        }

        [Fact(Timeout = 1500)]
        public void CallsOnErrorOnException()
        {
            // Arrange
            var mre = new ManualResetEventSlim(false);
            var onErrorCalled = false;
            var dbProviderFactory = new MockDbProviderFactory();
            var dbBehavior = new Mock<IDbBehavior>();
            dbBehavior.Setup(db => db.UpdateLoopRetryDelays).Returns(_defaultRetryDelays);
            dbBehavior.Setup(db => db.StartSqlDependencyListener()).Returns(false);
            dbProviderFactory.MockDataReader.Setup(r => r.Read()).Throws(new ApplicationException("test"));
            var operation = new ObservableDbOperation("test", "test", new TraceSource("test"), dbProviderFactory, dbBehavior.Object);
            operation.Faulted += _ =>
            {
                onErrorCalled = true;
                mre.Set();
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));
            mre.Wait();
            operation.Dispose();

            // Assert
            Assert.True(onErrorCalled);
        }

        private class MockDbProviderFactory : IDbProviderFactory
        {
            public MockDbProviderFactory()
            {
                MockDbConnection = new Mock<IDbConnection>();
                MockDbCommand = new Mock<IDbCommand>();
                MockDataReader = new Mock<IDataReader>();

                MockDbConnection.Setup(c => c.CreateCommand()).Returns(MockDbCommand.Object);
                MockDbCommand.SetupAllProperties();
                MockDbCommand.Setup(cmd => cmd.ExecuteReader()).Returns(MockDataReader.Object);
            }

            public Mock<IDbConnection> MockDbConnection { get; private set; }
            public Mock<IDbCommand> MockDbCommand { get; private set; }
            public Mock<IDataReader> MockDataReader { get; private set; }

            public IDbConnection CreateConnection()
            {
                return MockDbConnection.Object;
            }

            public virtual IDataParameter CreateParameter()
            {
                return new Mock<IDataParameter>().SetupAllProperties().Object;
            }
        }
    }
}
