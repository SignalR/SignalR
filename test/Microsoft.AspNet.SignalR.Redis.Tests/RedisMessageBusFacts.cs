// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.SignalR.Redis.Tests
{
    public class RedisMessageBusFacts
    {
        [Fact]
        public async void ConnectRetriesOnError()
        {
            int invokationCount = 0;
            var redisConnection = GetMockRedisConnection();

            var connectRetryTcs = new TaskCompletionSource<object>();

            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetCanceled();

            redisConnection.Setup(m => m.ConnectAsync(It.IsAny<string>(), It.IsAny<TraceSource>())).Returns<string, TraceSource>((connectionString, trace) =>
            {
                if (++invokationCount == 2)
                {
                    connectRetryTcs.SetResult(null);
                    return Task.FromResult(0);
                }
                else
                {
                    return tcs.Task;
                }
            });

            var redisMessageBus = GetMockRedisMessageBus(redisConnection);

            await redisMessageBus.Object.ConnectWithRetry();
            await connectRetryTcs.Task.OrTimeout();

            // Verify that the stream was opened.
            redisMessageBus.Verify(b => b.OpenStream(0));
        }

        [Fact]
        public async Task OpenCalledOnConnectionRestored()
        {
            int openInvoked = 0;

            var tcs = new TaskCompletionSource<object>();

            var redisConnection = GetMockRedisConnection();

            var redisMessageBus = GetMockRedisMessageBus(redisConnection);

            redisMessageBus.Setup(m => m.OpenStream(It.IsAny<int>())).Callback(() =>
            {
                // Open would be called twice - once when connection starts and once when it is restored
                if (++openInvoked == 2)
                {
                    tcs.SetResult(null);
                }
            });

            var instance = redisMessageBus.Object;
            await instance.ConnectWithRetry();

            redisConnection.Raise(mock => mock.ConnectionFailed += null, new Exception());
            redisConnection.Raise(mock => mock.ConnectionRestored += null, new Exception());

            await tcs.Task.OrTimeout();
        }

        [Fact]
        public async Task RestoreLatestValueForKeyCalledOnConnectionRestored()
        {
            bool restoreLatestValueForKey = false;

            var redisConnection = GetMockRedisConnection();

            redisConnection.Setup(m => m.RestoreLatestValueForKey(It.IsAny<int>(), It.IsAny<string>())).Returns(() =>
            {
                restoreLatestValueForKey = true;
                return TaskAsyncHelper.Empty;
            });

            var redisMessageBus = new RedisMessageBus(GetDependencyResolver(), new RedisScaleoutConfiguration(String.Empty, String.Empty),
            redisConnection.Object, connectAutomatically: false);

            await redisMessageBus.ConnectWithRetry();

            redisConnection.Raise(mock => mock.ConnectionFailed += null, new Exception());
            redisConnection.Raise(mock => mock.ConnectionRestored += null, new Exception());

            Assert.True(restoreLatestValueForKey, "RestoreLatestValueForKey not invoked");
        }

        [Fact]
        public void DatabaseFromConnectionstringIsUsed()
        {
            var configuration = new RedisScaleoutConfiguration("localhost,defaultDatabase=5", String.Empty);
            Assert.Equal(configuration.Database, 5);
        }

        [Fact]
        public async Task ConnectionRestoredWhileConnectionStarting()
        {
            var redisConnection = new Mock<IRedisConnection>();

            // Used to hold SubscribeAsync until we want to release it
            var subscribeTcs = new TaskCompletionSource<object>();

            // Used to signal back to the test code that we're in SubscribeAsync
            var atSubscribeTcs = new TaskCompletionSource<object>();

            // Used to signal end of connection restoration
            var connectionRestoredTcs = new TaskCompletionSource<object>();

            // Set up the connection to block SubscribeAsync
            // (which happens after the events are bound but before the state is updated)
            // until we say so.
            redisConnection
                .Setup(c => c.SubscribeAsync(It.IsAny<string>(), It.IsAny<Action<int, RedisMessage>>()))
                .Returns<string, Action<int, RedisMessage>>((_, __) =>
                {
                    atSubscribeTcs.SetResult(null);
                    return subscribeTcs.Task;
                });

            // ConnectAsync can proceed immediately though.
            redisConnection
                .Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<TraceSource>()))
                .Returns(Task.FromResult(0));

            // Use a Mock with CallBase because we need to hook OpenStream to figure out when we're finished with Reconnect
            var redisMessageBus = GetMockRedisMessageBus(redisConnection);

            // Connect but don't wait on it yet
            var connectionTask = redisMessageBus.Object.ConnectWithRetry();

            // Wait to hit SubscribeAsync
            await atSubscribeTcs.Task;

            // Now fire Connection Restored
            redisConnection.Raise(connection => connection.ConnectionRestored += null, new Exception());

            // Now allow the connection task to finish and wait for it
            subscribeTcs.SetResult(null);
            await connectionTask.OrTimeout();

            // Make sure OpenStream got called
            redisMessageBus.Verify(m => m.OpenStream(0));
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

        // We use a callbase mock because we want to check that OpenStream is called.
        private Mock<RedisMessageBus> GetMockRedisMessageBus(Mock<IRedisConnection> redisConnection)
        {
            return new Mock<RedisMessageBus>(
                GetDependencyResolver(),
                new RedisScaleoutConfiguration(String.Empty, String.Empty),
                redisConnection.Object,
                false)
            {
                CallBase = true
            };
        }
    }
}
