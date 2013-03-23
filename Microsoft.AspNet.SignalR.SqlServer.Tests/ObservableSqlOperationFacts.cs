using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UseSqlNotificationsIfAvailable(bool supportSqlNotifications)
        {
            // Arrange
            var sqlDependencyAdded = false;
            var retryLoopCount = 0;
            var mre = new ManualResetEventSlim();
            var dbProviderFactory = new Mock<DbProviderFactory>();
            var dbConnection = new Mock<IDbConnection>();
            var dbCommand = new Mock<IDbCommand>();
            var dbReader = new Mock<IDataReader>();
            var dbBehavior = new Mock<IDbBehavior>();
            dbProviderFactory.Setup(f => f.CreateConnection()).Returns(dbConnection.Object);
            dbConnection.Setup(c => c.CreateCommand()).Returns(dbCommand.Object);
            dbCommand.SetupAllProperties();
            dbCommand.Setup(cmd => cmd.ExecuteReader()).Returns(dbReader.Object);
            dbBehavior.Setup(db => db.IsRecoverableException(It.IsAny<Exception>())).Returns(true);
            dbBehavior.Setup(db => db.UpdateLoopRetryDelays).Returns(new [] { new [] { 0, 1 } });
            dbBehavior.Setup(db => db.StartSqlDependencyListener()).Returns(supportSqlNotifications);
            dbBehavior.Setup(db => db.AddSqlDependency(It.IsAny<IDbCommand>(), It.IsAny<Action<SqlNotificationEventArgs>>()))
                .Callback(() => {
                    sqlDependencyAdded = true;
                    mre.Set();
                });
            var operation = new ObservableDbOperation("", "", new TraceSource("test"), dbProviderFactory.Object, dbBehavior.Object)
            {
                OnError = ex => mre.Set(),
                OnRetryLoopIteration = () =>
                {
                    if (++retryLoopCount > 1)
                    {
                        mre.Set();
                    }
                }
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));
            mre.Wait(TimeSpan.FromMilliseconds(1000));
            //mre.Wait();
            operation.Dispose();

            // Assert
            Assert.Equal(supportSqlNotifications, sqlDependencyAdded);
        }

        [Theory]
        [InlineData(1, null, null)]
        [InlineData(5, null, null)]
        [InlineData(10, null, null)]
        [InlineData(1, 5, 10)]
        public void DoesRetryLoopConfiguredNumberOfTimes(int? length1, int? length2, int? length3)
        {
            // Arrange
            var dbProviderFactory = new Mock<DbProviderFactory>();
            var retryLoopCount = 0;
            var mre = new ManualResetEventSlim(false);
            var retryLoopArgs = new List<int?>(new[] { length1, length2, length3 }).Where(l => l.HasValue);
            var retryLoopTotal = retryLoopArgs.Sum().Value;
            var retryLoopDelays = retryLoopArgs.Select(l => new[] { 0, l.Value }).ToArray();
            var operation = new TestObservableDbOperation(dbProviderFactory.Object, retryLoopDelays)
            {
                SupportSqlNotifications = true,
                OnError = ex => mre.Set(),
                OnAddSqlDependency = () => mre.Set(),
                OnRetyLoopIteration = () =>
                {
                    if (++retryLoopCount == retryLoopTotal)
                    {
                        mre.Set();
                    }
                }
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));
            mre.Wait(TimeSpan.FromMilliseconds(1000));
            operation.Dispose();

            // Assert
            Assert.Equal(retryLoopTotal, retryLoopCount);
        }

        [Fact]
        public void CallsOnErrorOnFatalException()
        {
            // Arrange
            var dbProviderFactory = new Mock<DbProviderFactory>();
            var mre = new ManualResetEventSlim(false);
            var fatalExceptionThrown = false;
            var operation = new TestObservableDbOperation(dbProviderFactory.Object, new[] { new[] { 0, 1 } })
            {
                SupportSqlNotifications = false,
                OnException = ex => ex.Message.Equals("Recoverable", StringComparison.OrdinalIgnoreCase),
                OnError = ex =>
                {
                    if (ex != null)
                    {
                        fatalExceptionThrown = true;
                    }
                    mre.Set();
                },
                OnRetyLoopIteration = () =>
                {
                    throw new ApplicationException("Fatal");
                }
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));
            mre.Wait(TimeSpan.FromMilliseconds(1000));
            operation.Dispose();

            // Assert
            Assert.True(fatalExceptionThrown);
        }

        [Fact]
        public void ContinuesOnRecoverableExceptionInRetryLoop()
        {
            // Arrange
            var dbProviderFactory = new Mock<DbProviderFactory>();
            var mre = new ManualResetEventSlim(false);
            var fatalExceptionThrown = false;
            var retryLoopCount = 0;
            var operation = new TestObservableDbOperation(dbProviderFactory.Object, new[] { new[] { 0, 1 } })
            {
                SupportSqlNotifications = false,
                RetryDelay = TimeSpan.FromMilliseconds(0),
                OnException = ex => ex.Message.Equals("Recoverable", StringComparison.OrdinalIgnoreCase),
                OnError = ex =>
                {
                    if (ex != null)
                    {
                        fatalExceptionThrown = true;
                    }
                    mre.Set();
                },
                OnRetyLoopIteration = () =>
                {
                    if (++retryLoopCount == 2)
                    {
                        throw new ApplicationException("Recoverable");
                    }
                },
                OnRetry = _ => mre.Set()
            };

            // Act
            ThreadPool.QueueUserWorkItem(_ => operation.ExecuteReaderWithUpdates((record, o) => { }));
            mre.Wait(TimeSpan.FromMilliseconds(1000));
            operation.Dispose();

            // Assert
            Assert.False(fatalExceptionThrown);
        }

        private class TestObservableDbOperation : ObservableDbOperation
        {
            private int[][] _updateLoopRetryDelays = new[] { new[] { 0, 1 } };

            public TestObservableDbOperation(DbProviderFactory dbProviderFactory)
                : base("test-connection-string", "test-command-text", new TraceSource("test"), dbProviderFactory, null)
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

            public Func<Exception, bool> OnException { get; set; }

            protected override int ExecuteReader(Action<IDataRecord, DbOperation> processRecord, Action<IDbCommand> commandAction)
            {
                while (true)
                {
                    try
                    {
                        if (commandAction != null)
                        {
                            // If there's a command here it isn't a retry loop, it's setting up a query notification
                            commandAction(null);
                        }
                        else
                        {
                            if (OnRetyLoopIteration != null)
                            {
                                OnRetyLoopIteration();
                            }
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!IsRecoverableException(ex))
                        {
                            throw;
                        }
                        OnRetry(null);
                    }
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

            protected override bool IsRecoverableException(Exception exception)
            {
                if (OnException != null)
                {
                    return OnException(exception);
                }
                return base.IsRecoverableException(exception);
            }
        }
    }
}
