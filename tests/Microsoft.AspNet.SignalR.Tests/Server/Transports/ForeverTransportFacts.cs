using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Transports
{
    public class ForeverTransportFacts
    {
        [Fact]
        public void SendUrlTriggersReceivedEvent()
        {
            var tcs = new TaskCompletionSource<string>();
            var request = new Mock<IRequest>();
            var form = new NameValueCollection();
            form["data"] = "This is my data";
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.ReadForm()).Returns(Task.FromResult<INameValueCollection>(new NameValueCollectionWrapper(form)));
            request.Setup(m => m.LocalPath).Returns("/test/echo/send");
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = JsonUtility.CreateDefaultSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));
            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            transport.Object.Received = data =>
            {
                tcs.TrySetResult(data);
                return TaskAsyncHelper.Empty;
            };

            transport.Object.ProcessRequest(transportConnection.Object).Wait();

            Assert.Equal("This is my data", tcs.Task.Result);
        }

        [Fact]
        public void AbortUrlTriggersConnectionAbort()
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/abort");
            string abortedConnectionId = null;
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = JsonUtility.CreateDefaultSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));
            transportConnection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()))
                               .Callback<ConnectionMessage>(m =>
                               {
                                   abortedConnectionId = m.Signal;
                                   var command = m.Value as Command;
                                   Assert.NotNull(command);
                                   Assert.Equal(CommandType.Abort, command.CommandType);
                               })
                               .Returns(TaskAsyncHelper.Empty);

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            transport.Object.ConnectionId = "1";
            transport.Object.ProcessRequest(transportConnection.Object).Wait();

            Assert.Equal("c-1", abortedConnectionId);
        }

        [Fact]
        public void AvoidDeadlockIfCancellationTokenTriggeredBeforeSubscribing()
        {
            var response = new Mock<IResponse>();
            response.Setup(m => m.CancellationToken).Returns(CancellationToken.None);
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/connect");
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = JsonUtility.CreateDefaultSerializer();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            Func<PersistentResponse, object, Task<bool>> callback = null;
            object state = null;

            var disposable = new DisposableAction(() =>
            {
                callback(new PersistentResponse() { Terminal = true }, state);
            });

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Callback<string, Func<PersistentResponse, object, Task<bool>>, int, object>((id, cb, max, st) =>
                                                     {
                                                         callback = cb;
                                                         state = st;
                                                     })
                                                     .Returns(disposable);

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            var wh = new ManualResetEventSlim();

            transport.Object.BeforeCancellationTokenCallbackRegistered = () =>
            {
                // Trip the cancellation token
                transport.Object.End();
            };

            // Act
            Task.Factory.StartNew(() =>
            {
                transport.Object.ProcessRequest(transportConnection.Object);
                wh.Set();
            });

            Assert.True(wh.Wait(TimeSpan.FromSeconds(2)), "Dead lock!");
        }

        [Fact]
        public void ReceiveThrowingReturnsFaultedTask()
        {
            var response = new Mock<IResponse>();
            response.Setup(m => m.CancellationToken).Returns(CancellationToken.None);
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/connect");
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = JsonUtility.CreateDefaultSerializer();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Throws(new InvalidOperationException());

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            // Act
            var task = transport.Object.ProcessRequest(transportConnection.Object);

            // Assert
            Assert.Throws<AggregateException>(() => task.Wait(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void RunPostReceiveWithFaultedTask()
        {
            RunWithPostReceive(() => TaskAsyncHelper.FromError(new Exception()));
        }

        [Fact]
        public void RunPostReceiveWithCancelledTask()
        {
            Func<Task> cancelled = () =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetCanceled();
                return tcs.Task;
            };


            RunWithPostReceive(cancelled);
        }

        [Fact]
        public void RunPostReceiveWithSuccessfulTask()
        {
            RunWithPostReceive(() => TaskAsyncHelper.Empty);
        }

        [Fact]
        public void ReceiveAbortBeforeCancellationSetup()
        {
            var response = new Mock<IResponse>();
            response.Setup(m => m.CancellationToken).Returns(CancellationToken.None);
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/connect");
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = JsonUtility.CreateDefaultSerializer();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            counters.SetupGet(m => m.ConnectionsDisconnected).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            Func<PersistentResponse, object, Task<bool>> callback = null;
            object state = null;

            var disposable = new DisposableAction(() =>
            {
                callback(new PersistentResponse() { Terminal = true }, state);
            });

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Callback<string, Func<PersistentResponse, object, Task<bool>>, int, object>(async (id, cb, max, st) =>
                                                     {
                                                         callback = cb;
                                                         state = st;

                                                         bool result = await cb(new PersistentResponse() { Aborted = true } , st);

                                                         if (!result)
                                                         {
                                                             disposable.Dispose();
                                                         }
                                                     })
                                                    .Returns(disposable);
            transportConnection.Setup(m => m.Send(It.IsAny<ConnectionMessage>())).Returns(TaskAsyncHelper.Empty);

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            transport.Setup(m => m.Send(It.IsAny<PersistentResponse>())).Returns(TaskAsyncHelper.Empty);

            bool ended = false;

            transport.Object.Connected = () => TaskAsyncHelper.Empty;

            transport.Object.AfterRequestEnd = (ex) =>
            {
                Assert.Null(ex);
                ended = true;
            };

            // Act
            transport.Object.ProcessRequest(transportConnection.Object);

            // Assert
            Assert.True(ended);
        }

        public void RunWithPostReceive(Func<Task> postReceive)
        {
            var response = new Mock<IResponse>();
            response.Setup(m => m.CancellationToken).Returns(CancellationToken.None);
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/connect");
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = JsonUtility.CreateDefaultSerializer();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Returns(DisposableAction.Empty);

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            transport.Object.Connected = postReceive;

            // Act
            transport.Object.ProcessRequest(transportConnection.Object);

            // Assert
            try
            {
                Assert.True(transport.Object.ConnectTask.Wait(TimeSpan.FromSeconds(2)), "ConnectTask task not tripped");
            }
            catch
            {
            }
        }

        [Fact]
        public void RequestCompletesAfterCompletedWritesInTaskQueue()
        {
            EnqueAsyncWriteAndEndRequest(() => TaskAsyncHelper.Empty);
        }

        [Fact]
        public void RequestCompletesAfterCancelledWritesInTaskQueue()
        {
            Func<Task> writeCancelled = () =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetCanceled();
                return tcs.Task;
            };

            EnqueAsyncWriteAndEndRequest(writeCancelled);
        }

        [Fact]
        public void RequestCompletesAfterFaultedWritesInTaskQueue()
        {
            Func<Task> writeFaulted = () => TaskAsyncHelper.FromError(new Exception());
            EnqueAsyncWriteAndEndRequest(writeFaulted);
        }

        [Fact]
        public void InitializeResponseIsFirstEnqueuedOperation()
        {
            // Arrange
            var response = new Mock<IResponse>();
            response.Setup(m => m.CancellationToken).Returns(CancellationToken.None);
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/connect");
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonSerializer();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            counters.SetupGet(m => m.ErrorsTransportTotal).Returns(new NoOpPerformanceCounter());
            counters.SetupGet(m => m.ErrorsTransportPerSec).Returns(new NoOpPerformanceCounter());
            counters.SetupGet(m => m.ErrorsAllTotal).Returns(new NoOpPerformanceCounter());
            counters.SetupGet(m => m.ErrorsAllPerSec).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>()))
                .Returns<string, Func<PersistentResponse, object, Task<bool>>, int, object>(
                    (messageId, callback, maxMessages, state) => 
                    {
                        callback(new PersistentResponse(), state);
                        return DisposableAction.Empty;
                    });

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            var queue = new TaskQueue();
            var results = new List<string>();

            transport.Setup(t => t.EnqueueOperation(It.IsAny<Func<object, Task>>(), It.IsAny<object>()))
                .Returns<Func<object, Task>, object>(
                    (writeAsync, state) =>
                    {
                        return queue.Enqueue(writeAsync, state);
                    });

            transport.Setup(t => t.InitializeResponse(It.IsAny<ITransportConnection>()))
                .Returns<ITransportConnection>(
                    pr =>
                    {
                        results.Add("InitializeResponse");
                        return TaskAsyncHelper.Empty;
                    });

            transport.Setup(t => t.Send(It.IsAny<PersistentResponse>()))
                .Returns<PersistentResponse>(
                    pr =>
                        transport.Object.EnqueueOperation(() =>
                        {
                            results.Add("Send");
                            return TaskAsyncHelper.Empty;
                        }));

            // Act
            transport.Object.ProcessRequest(transportConnection.Object);

            // Assert
            Assert.Equal("InitializeResponse", results[0]);
            Assert.Equal("Send", results[1]);
        }

        [Fact]
        public void RequestCompletesAfterFaultedInitializeResponse()
        {
            // Arrange
            var response = new Mock<IResponse>();
            response.Setup(m => m.CancellationToken).Returns(CancellationToken.None);
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/connect");
            var counters = new PerformanceCounterManager();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonSerializer();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>()))
                .Returns<string, Func<PersistentResponse, object, Task<bool>>, int, object>(
                    (messageId, callback, maxMessages, state) =>
                        new DisposableAction(() => callback(new PersistentResponse(), state))
                    );

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters, traceManager.Object)
            {
                CallBase = true
            };

            var queue = new TaskQueue();

            transport.Setup(t => t.EnqueueOperation(It.IsAny<Func<object, Task>>(), It.IsAny<object>()))
                .Returns<Func<object, Task>, object>(
                    (writeAsync, state) => queue.Enqueue(writeAsync, state));

            transport.Setup(t => t.InitializeResponse(It.IsAny<ITransportConnection>()))
                .Returns<ITransportConnection>(
                    pr => TaskAsyncHelper.FromError(new Exception()));

            transport.Setup(t => t.Send(It.IsAny<PersistentResponse>()))
                .Returns<PersistentResponse>(
                    pr => transport.Object.EnqueueOperation(() => TaskAsyncHelper.Empty));

            var tcs = new TaskCompletionSource<bool>();
            transport.Object.AfterRequestEnd = (ex) =>
            {
                // Trip the cancellation token
                tcs.TrySetResult(transport.Object.WriteQueue.IsDrained);
            };

            // Act
            transport.Object.ProcessRequest(transportConnection.Object);

            // Assert
            Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(2)));
            Assert.True(tcs.Task.Result);
        }

        public void EnqueAsyncWriteAndEndRequest(Func<Task> writeAsync)
        {
            var response = new Mock<IResponse>();
            response.Setup(m => m.CancellationToken).Returns(CancellationToken.None);
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));
            request.Setup(m => m.LocalPath).Returns("/test/echo/connect");
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = JsonUtility.CreateDefaultSerializer();
            var hostContext = new HostContext(request.Object, response.Object);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            counters.SetupGet(m => m.ConnectionsConnected).Returns(new NoOpPerformanceCounter());
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            Func<PersistentResponse, object, Task<bool>> callback = null;
            object state = null;

            var disposable = new DisposableAction(() =>
            {
                callback(new PersistentResponse() { Terminal = true }, state);
            });

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Callback<string, Func<PersistentResponse, object, Task<bool>>, int, object>((id, cb, max, st) =>
                                                     {
                                                         callback = cb;
                                                         state = st;
                                                     })
                                                     .Returns(disposable);
            transportConnection.Setup(m => m.Send(It.IsAny<ConnectionMessage>())).Returns(TaskAsyncHelper.Empty);

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            transport.Setup(m => m.CancellationToken).Returns(CancellationToken.None);

            var tcs = new TaskCompletionSource<bool>();

            transport.Object.EnqueueOperation(writeAsync);

            transport.Object.Connected = () => TaskAsyncHelper.Empty;

            transport.Object.AfterRequestEnd = (ex) =>
            {
                // Trip the cancellation token
                tcs.TrySetResult(transport.Object.WriteQueue.IsDrained);
            };

            transport.Object.BeforeCancellationTokenCallbackRegistered = () =>
            {
                transport.Object.End();
            };

            Assert.True(transport.Object.ProcessRequest(transportConnection.Object).Wait(TimeSpan.FromSeconds(2)));
            Assert.True(tcs.Task.Result);
        }
    }
}
