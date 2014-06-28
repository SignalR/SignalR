using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Redis;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Scaleout
{
    public class RedisMessageBusFacts
    {
        [Fact]
        public void ConnectRetriesOnError()
        {
            int invokationCount = 0;
            var wh = new ManualResetEventSlim();
            var redisConnection = GetMockRedisConnection();

            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetCanceled();

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>())).Returns<string>(connectionString =>
            {
                if (++invokationCount == 2)
                {
                    wh.Set();
                    return Task.FromResult(0);
                }
                else
                {
                    return tcs.Task;
                }
            });

            var redisMessageBus = new RedisMessageBus(GetDependencyResolver(), new RedisScaleoutConfiguration(String.Empty, String.Empty),
            redisConnection.Object);

            Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));

            redisMessageBus.InitialConnectTask.Wait();
            Assert.Equal(RedisMessageBus.State.Connected, redisMessageBus.ConnectionState);
        }

        [Fact]
        public void OpenCalledOnConnectionRestored()
        {
            int openInvoked = 0;
            var wh = new ManualResetEventSlim();

            var redisConnection = GetMockRedisConnection();

            redisConnection.Setup(m => m.RestoreLatestValueForKey(It.IsAny<TraceSource>())).Returns(TaskAsyncHelper.Empty);

            var redisMessageBus = new Mock<RedisMessageBus>(GetDependencyResolver(), new RedisScaleoutConfiguration(String.Empty, String.Empty),
                redisConnection.Object) { CallBase = true };

            redisMessageBus.Setup(m => m.OpenStream(It.IsAny<int>())).Callback(() =>
            {
                // Open would be called twice - once when connection starts and once when it is restored
                if (++openInvoked == 2)
                {
                    wh.Set();
                }
            });

            // Creating an instance to invoke the constructor which starts the connection
            var instance = redisMessageBus.Object;

            redisMessageBus.Object.InitialConnectTask.Wait();

            redisConnection.Raise(mock => mock.ConnectionRestored += null, new Exception());

            Assert.Equal(openInvoked, 2);
        }

        [Fact]
        public void RestoreLatestValueForKeyCalledOnConnectionRestored()
        {
            bool restoreLatestValueForKey = false;

            var redisConnection = GetMockRedisConnection();

            redisConnection.Setup(m => m.RestoreLatestValueForKey(It.IsAny<TraceSource>())).Returns(() =>
            {
                restoreLatestValueForKey = true;
                return TaskAsyncHelper.Empty;
            });

            var redisMessageBus = new Mock<RedisMessageBus>(GetDependencyResolver(), new RedisScaleoutConfiguration(String.Empty, String.Empty),
                redisConnection.Object) { CallBase = true };

            // Creating an instance to invoke the constructor which starts the connection
            var instance = redisMessageBus.Object;

            redisMessageBus.Object.InitialConnectTask.Wait();

            redisConnection.Raise(mock => mock.ConnectionRestored += null, new Exception());

            Assert.True(restoreLatestValueForKey, "RestoreLatestValueForKey not invoked");
        }

        [Fact]
        public void ConnectionFailedChangesStateToClosed()
        {
            var redisConnection = GetMockRedisConnection();

            var redisMessageBus = new RedisMessageBus(GetDependencyResolver(),
                new RedisScaleoutConfiguration(String.Empty, String.Empty),
                redisConnection.Object);

            redisMessageBus.InitialConnectTask.Wait();

            Assert.Equal(RedisMessageBus.State.Connected, redisMessageBus.ConnectionState);

            redisConnection.Raise(mock => mock.ConnectionFailed += null, new Exception("Test exception"));

            Assert.Equal(RedisMessageBus.State.Closed, redisMessageBus.ConnectionState);
        }

        private Mock<IRedisConnection> GetMockRedisConnection()
        {
            var redisConnection = new Mock<IRedisConnection>();

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            redisConnection.Setup(m => m.SubscribeAsync(It.IsAny<string>(), It.IsAny<Action<int, RedisMessage>>()))
                .Returns(Task.FromResult(0));

            return redisConnection;
        }

        private DefaultDependencyResolver GetDependencyResolver()
        {
            var dr = new DefaultDependencyResolver();
            dr.Register(typeof(ITraceManager), () => { return new TraceManager(); });
            return dr;
        }
    }
}
