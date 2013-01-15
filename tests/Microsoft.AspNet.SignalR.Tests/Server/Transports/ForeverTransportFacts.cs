using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
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
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Form).Returns(form);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/send"));
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
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
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/abort"));
            string abortedConnectionId = null;
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
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

            transport.Object.ProcessRequest(transportConnection.Object).Wait();

            Assert.Equal("1", abortedConnectionId);
        }

        [Fact]
        public void AvoidDeadlockIfCancellationTokenTriggeredBeforeSubscribing()
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/connect"));
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            Func<PersistentResponse, Task<bool>> callback = null;

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, Task<bool>>>(),
                                                     It.IsAny<int>())).Callback<string, Func<PersistentResponse, Task<bool>>, int>((id, cb, max) =>
                                                     {
                                                         callback = cb;
                                                     })
                                                     .Returns(new DisposableAction(() =>
                                                     {
                                                         callback(new PersistentResponse());
                                                     }));

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
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/connect"));
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, Task<bool>>>(),
                                                     It.IsAny<int>())).Throws(new InvalidOperationException());

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

        public void RunWithPostReceive(Func<Task> postReceive)
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/connect"));
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, Task<bool>>>(),
                                                     It.IsAny<int>())).Returns(DisposableAction.Empty);

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            transport.Object.Connected = postReceive;

            // Act
            transport.Object.ProcessRequest(transportConnection.Object);

            // Assert
            Assert.True(transport.Object.InitializeTcs.Task.Wait(TimeSpan.FromSeconds(2)), "Initialize task not tripped");
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

        public void EnqueAsyncWriteAndEndRequest(Func<Task> writeAsync)
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            request.Setup(m => m.QueryString).Returns(qs);
            request.Setup(m => m.Url).Returns(new Uri("http://test/echo/connect"));
            var counters = new Mock<IPerformanceCounterManager>();
            var heartBeat = new Mock<ITransportHeartbeat>();
            var json = new JsonNetSerializer();
            var hostContext = new HostContext(request.Object, null);
            var transportConnection = new Mock<ITransportConnection>();
            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(m => m[It.IsAny<string>()]).Returns(new System.Diagnostics.TraceSource("foo"));

            Func<PersistentResponse, Task<bool>> callback = null;

            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, Task<bool>>>(),
                                                     It.IsAny<int>())).Callback<string, Func<PersistentResponse, Task<bool>>, int>((id, cb, max) =>
                                                     {
                                                         callback = cb;
                                                     })
                                                     .Returns(new DisposableAction(() =>
                                                     {
                                                         callback(new PersistentResponse());
                                                     }));

            var transport = new Mock<ForeverTransport>(hostContext, json, heartBeat.Object, counters.Object, traceManager.Object)
            {
                CallBase = true
            };

            transport.Setup(m => m.IsAlive).Returns(true);

            var tcs = new TaskCompletionSource<bool>();

            transport.Object.EnqueueOperation(writeAsync);

            transport.Object.AfterRequestEnd = () =>
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
