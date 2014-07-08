using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Redis.Tests
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

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<TraceSource>())).Returns<string, TraceSource>((connectionString, trace) =>
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
            redisConnection.Object, false);

            redisMessageBus.ConnectWithRetry().Wait();

            Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal(RedisMessageBus.State.Connected, redisMessageBus.ConnectionState);
        }

        [Fact]
        public void OpenCalledOnConnectionRestored()
        {
            int openInvoked = 0;
            var wh = new ManualResetEventSlim();

            var redisConnection = GetMockRedisConnection();

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

            redisConnection.Raise(mock => mock.ConnectionRestored += null, new Exception());

            Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void ConnectionFailedChangesStateToClosed()
        {
            var redisConnection = GetMockRedisConnection();

            var redisMessageBus = new RedisMessageBus(GetDependencyResolver(),
                new RedisScaleoutConfiguration(String.Empty, String.Empty),
                redisConnection.Object, false);

            redisMessageBus.ConnectWithRetry().Wait();

            Assert.Equal(RedisMessageBus.State.Connected, redisMessageBus.ConnectionState);

            redisConnection.Raise(mock => mock.ConnectionFailed += null, new Exception("Test exception"));

            Assert.Equal(RedisMessageBus.State.Closed, redisMessageBus.ConnectionState);
        }

        private Mock<IRedisConnection> GetMockRedisConnection()
        {
            var redisConnection = new Mock<IRedisConnection>();

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<TraceSource>()))
                .Returns(Task.FromResult(0));

            redisConnection.Setup(m => m.SubscribeAsync(It.IsAny<string>(), It.IsAny<Action<int, RedisMessage>>()))
                .Returns(Task.FromResult(0));

            return redisConnection;
        }

        private DefaultDependencyResolver GetDependencyResolver()
        {
            var dr = new DefaultDependencyResolver();
            var traceManager = new TraceManager();
            dr.Register(typeof(ITraceManager), () => traceManager);
            return dr;
        }
    }
}
