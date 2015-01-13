// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.
extern alias StoreClient;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.AspNet.SignalR.Client.Store.Tests;
using Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes;
using System;
using System.Threading;
using Xunit;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransportFacts
    {
        [Fact]
        public void CannotCreateWebSocketTransportWithNullHttpClient()
        {
            Assert.Equal(
                "httpClient",
                Assert.Throws<ArgumentNullException>(() => new WebSocketTransport(null)).ParamName);
        }

        [Fact]
        public void NameReturnsCorrectTransportName()
        {
            Assert.Equal("webSockets", new WebSocketTransport().Name);
        }

        [Fact]
        public void SupportsKeepAliveReturnsTrue()
        {
            Assert.True(new WebSocketTransport().SupportsKeepAlive);
        }

        [Fact]
        public async Task StartsValidatesInputParameters()
        {
            Assert.Equal("connection",
                (await Assert.ThrowsAsync<ArgumentNullException>(async () => await
                    new WebSocketTransport().Start(null, null, CancellationToken.None))).ParamName);
        }

        [Fact]
        public async Task StartCreatesAndOpensWebSocket()
        {
            var fakeWebSocketTransport = new FakeWebSocketTransport();

            fakeWebSocketTransport.Setup("OpenWebSocket", () =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.TrySetResult(null);
                return tcs.Task;
            });

            var fakeConnection = new FakeConnection
            {
                TransportConnectTimeout = new TimeSpan(0, 0, 0, 0, 100),
                TotalTransportConnectTimeout = new TimeSpan(0, 0, 0, 0, 100),
                Url = "http://fake.url",
                Protocol = new Version(1, 42),
                ConnectionToken = "MyConnToken",
                MessageId = "MsgId"
            };

            // connect timeout unblocks this call hence the expected exception
            await Assert.ThrowsAsync<TimeoutException>(
                async () => await fakeWebSocketTransport.Start(fakeConnection, "test", CancellationToken.None));

            Assert.Equal(1, fakeConnection.GetInvocations("Trace").Count());
            Assert.Equal(1, fakeConnection.GetInvocations("PrepareRequest").Count());

            var openWebSocketInvocations = fakeWebSocketTransport.GetInvocations("OpenWebSocket").ToArray();
            Assert.Equal(1, openWebSocketInvocations.Length);
            Assert.StartsWith(
                "ws://fake.urlconnect/?clientProtocol=1.42&transport=webSockets&connectionData=test&connectionToken=MyConnToken&messageId=MsgId&noCache=",
                ((Uri)openWebSocketInvocations[0][1]).AbsoluteUri);
        }

        [Fact]
        public async Task InCaseOfExceptionStartInvokesOnFailureAndThrowsOriginalException()
        {
            var fakeConnection = new FakeConnection { TotalTransportConnectTimeout = new TimeSpan(0, 0, 10)};
            var fakeWebSocketTransport = new FakeWebSocketTransport();  

            var expectedException = new Exception("OpenWebSocket failed.");
            fakeWebSocketTransport.Setup<Task>("OpenWebSocket", () =>
            {
                throw expectedException;
            });

            Assert.Same(expectedException,
                await Assert.ThrowsAsync<Exception>(
                    async () => await fakeWebSocketTransport.Start(fakeConnection, null, CancellationToken.None)));

            Assert.Equal(1, fakeWebSocketTransport.GetInvocations("OnStartFailed").Count());
        }

        [Fact]
        public async Task StartInvokesOnFailureAndThrowsIfTaskCancelled()
        {
            var fakeConnection = new FakeConnection {TotalTransportConnectTimeout = new TimeSpan(0, 0, 10)};
            var fakeWebSocketTransport = new FakeWebSocketTransport();
            var cancellationTokenSource = new CancellationTokenSource();

            fakeWebSocketTransport.Setup<Task>("OpenWebSocket", () =>
            {
                cancellationTokenSource.Cancel();

                var tcs = new TaskCompletionSource<object>();
                tcs.TrySetResult(null);
                return tcs.Task;
            });

            Assert.Equal(
                ResourceUtil.GetResource("Error_ConnectionCancelled"),
                (await Assert.ThrowsAsync<OperationCanceledException>(
                    async () => await fakeWebSocketTransport.Start(fakeConnection, null, cancellationTokenSource.Token))).Message);

            Assert.Equal(1, fakeWebSocketTransport.GetInvocations("OnStartFailed").Count());
        }

        [Fact]
        public async Task StartInvokesOnFailureAndThrowsIfOpenWebSocketCancelled()
        {
            var fakeConnection = new FakeConnection { TotalTransportConnectTimeout = new TimeSpan(0, 0, 10) };
            var fakeWebSocketTransport = new FakeWebSocketTransport();

            fakeWebSocketTransport.Setup<Task>("OpenWebSocket", () =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetCanceled();
                return tcs.Task;
            });

            Assert.Equal(
                ResourceUtil.GetResource("Error_TransportFailedToConnect"),
                (await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await fakeWebSocketTransport.Start(fakeConnection, null, CancellationToken.None))).Message);

            Assert.Equal(1, fakeWebSocketTransport.GetInvocations("OnStartFailed").Count());
        }

        [Fact]
        public void MessageReceivedReadsAndProcessesMessages()
        {
            var fakeDataReader = new FakeDataReader
            {
                UnicodeEncoding = (UnicodeEncoding)(-1),
                UnconsumedBufferLength = 42
            };
            fakeDataReader.Setup("ReadString", () => "MessageBody");

            var fakeWebSocketResponse = new FakeWebSocketResponse();
            fakeWebSocketResponse.Setup("GetDataReader", () => fakeDataReader);

            var transport = new WebSocketTransport();
            transport.Start(new FakeConnection(), string.Empty, CancellationToken.None);

            var fakeConnection = new FakeConnection();
            transport.MessageReceived(fakeWebSocketResponse, fakeConnection);

            Assert.Equal(UnicodeEncoding.Utf8, fakeDataReader.UnicodeEncoding);
            fakeDataReader.Verify("ReadString", new List<object[]> {new object[] { 42u}});

            // invoked by ProcessResponse
            Assert.Equal(1, fakeConnection.GetInvocations("MarkLastMessage").Count());
            Assert.Equal(1, fakeConnection.GetInvocations("Trace").Count());
        }

        [Fact]
        public async Task SendChecksInputArguments()
        {
            Assert.Equal("connection",
               (await Assert.ThrowsAsync<ArgumentNullException>(async () => await
                   new WebSocketTransport().Send(null, null, null))).ParamName);
        }

        [Fact]
        public async Task SendWritesToWebSocketOutputStream()
        {
            var fakeOutputStream = new FakeOutputStream();
            var fakeWebSocket = new FakeWebSocket { OutputStream = fakeOutputStream };

            await WebSocketTransport.Send(fakeWebSocket, "42.42");

            var writeAsyncInvocations = fakeOutputStream.GetInvocations("WriteAsync").ToArray();

            Assert.Equal(1, writeAsyncInvocations.Length);
            Assert.Equal(5u, ((IBuffer)writeAsyncInvocations[0][0]).Length);
        }

        [Fact(Skip = "xUnit AccessViolationException https://github.com/xunit/xunit/issues/190 when running with MsBuild. " +
                     "Note: This test still can be run in VS.")]
        public async Task CannotInvokeSendIfWebSocketUnitialized()
        {
            var fakeConnection = new FakeConnection {State = ConnectionState.Disconnected};

            Assert.Equal(
                StoreClient::Microsoft.AspNet.SignalR.Client.Resources.GetResourceString("Error_WebSocketUninitialized"),
                (await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await new WebSocketTransport().Send(fakeConnection, null, null))).Message);
        }

        [Fact]
        public async Task ReconnectStartsNewWebSocket()
        {
            var fakeConnection = new FakeConnection
            {
                LastActiveAt = DateTime.Now.AddDays(1),
                ReconnectWindow = new TimeSpan(0, 0, 0),
                Url = "http://fakeserver/"
            };

            fakeConnection.Setup("ChangeState",
                () =>
                {
                    fakeConnection.State = ConnectionState.Reconnecting;
                    return true;
                });

            var fakeWebSocketTransport = new FakeWebSocketTransport();
            fakeWebSocketTransport.Setup<Task>("OpenWebSocket", () =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.TrySetResult(null);
                return tcs.Task;
            });

            await fakeWebSocketTransport.Reconnect(fakeConnection, "abc");

            var openWebSocketInvocations = fakeWebSocketTransport.GetInvocations("OpenWebSocket").ToArray();
            Assert.Equal(1, openWebSocketInvocations.Length);
            Assert.StartsWith("ws://fakeserver/reconnect?", ((Uri)openWebSocketInvocations[0][1]).AbsoluteUri);
            Assert.Contains("&connectionData=abc&", ((Uri)openWebSocketInvocations[0][1]).AbsoluteUri);
            Assert.Equal(1, fakeConnection.GetInvocations("OnReconnected").Count());
        }

        [Fact]
        public async Task ReconnectStopsConnectionAndDoesNotStartNewWebSocketIfReconnectWindowExceeded()
        {
            var fakeConnection = new FakeConnection
            {
                LastActiveAt = DateTime.UtcNow.AddSeconds(-10),
                ReconnectWindow = new TimeSpan(0, 0, 5),
                Url = "http://fakeserver/"
            };

            fakeConnection.Setup("ChangeState",
                () =>
                {
                    fakeConnection.State = ConnectionState.Reconnecting;
                    return true;
                });

            var fakeWebSocketTransport = new FakeWebSocketTransport();

            await fakeWebSocketTransport.Reconnect(fakeConnection, null);

            Assert.Equal(0, fakeWebSocketTransport.GetInvocations("OpenWebSocket").Count());
            var stopInvocations = fakeConnection.GetInvocations("Stop").ToArray();
            Assert.Equal(1, stopInvocations.Length);
            Assert.IsType(typeof(TimeoutException), stopInvocations[0][0]);
        }

        [Fact]
        public async Task ReconnectDoesNotStartNewWebSocketIfClientWasNotInConnectState()
        {
            var fakeConnection = new FakeConnection
            {
                LastActiveAt = DateTime.UtcNow,
                ReconnectWindow = new TimeSpan(0, 0, 15),
                Url = "http://fakeserver/"
            };

            fakeConnection.Setup("ChangeState",
                () =>
                {
                    fakeConnection.State = ConnectionState.Disconnected;
                    return false;
                });

            var fakeWebSocketTransport = new FakeWebSocketTransport();

            await fakeWebSocketTransport.Reconnect(fakeConnection, null);

            Assert.Equal(0, fakeWebSocketTransport.GetInvocations("OpenWebSocket").Count());
            Assert.Equal(0, fakeConnection.GetInvocations("Stop").Count());
        }

        [Fact]
        public async Task ReconnectDoesNotStartNewWebSocketIfDisconnectTokenTripped()
        {
            var fakeConnection = new FakeConnection
            {
                LastActiveAt = DateTime.UtcNow,
                ReconnectWindow = new TimeSpan(0, 0, 15),
                Url = "http://fakeserver/",
                State = ConnectionState.Reconnecting
            };

            var fakeWebSocketTransport = new FakeWebSocketTransport();

            // this is called just to initialize the disconnect token
            var _ = fakeWebSocketTransport.Start(new FakeConnection(), string.Empty, new CancellationToken(true));

            await fakeWebSocketTransport.Reconnect(fakeConnection, null);

            Assert.Equal(0, fakeWebSocketTransport.GetInvocations("OpenWebSocket").Count());
            Assert.Equal(0, fakeConnection.GetInvocations("Stop").Count());
        }

        [Fact]
        public async Task ReconnectRetriesReconnectingIfStartingWebSocketThrows()
        {
            var fakeConnection = new FakeConnection
            {
                LastActiveAt = DateTime.UtcNow,
                ReconnectWindow = new TimeSpan(0, 0, 15),
                Url = "http://fakeserver/"
            };

            fakeConnection.Setup("ChangeState",
                () =>
                {
                    fakeConnection.State = ConnectionState.Reconnecting;
                    return true;
                });

            var fakeWebSocketTransport = new FakeWebSocketTransport
            {
                ReconnectDelay = new TimeSpan(0, 0, 0, 1)
            };

            var openWebSocketInvoked = false;
            var exception = new Exception();
            fakeWebSocketTransport.Setup<Task>("OpenWebSocket", () =>
            {
                if (!openWebSocketInvoked)
                {
                    openWebSocketInvoked = true;
                    throw exception;
                }

                var tcs = new TaskCompletionSource<object>();
                tcs.TrySetResult(null);
                return tcs.Task;
            });

            await fakeWebSocketTransport.Reconnect(fakeConnection, null);

            Assert.Equal(2, fakeWebSocketTransport.GetInvocations("OpenWebSocket").Count());
            var onErrorInvocations = fakeConnection.GetInvocations("OnError").ToArray();
            Assert.Equal(1, onErrorInvocations.Length);
            Assert.Same(exception, onErrorInvocations[0][0]);
            Assert.Equal(1, fakeConnection.GetInvocations("OnReconnected").Count());
        }

        [Fact]
        public async Task ReconnectStopsReconnectingIfStartingWebSocketCancelled()
        {
            var fakeConnection = new FakeConnection
            {
                LastActiveAt = DateTime.UtcNow,
                ReconnectWindow = new TimeSpan(0, 0, 15),
                Url = "http://fakeserver/"
            };

            fakeConnection.Setup("ChangeState",
                () =>
                {
                    fakeConnection.State = ConnectionState.Reconnecting;
                    return true;
                });

            var fakeWebSocketTransport = new FakeWebSocketTransport
            {
                ReconnectDelay = new TimeSpan(0, 0, 0, 1)
            };

            fakeWebSocketTransport.Setup<Task>("OpenWebSocket", () =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetCanceled();
                return tcs.Task;
            });

            await fakeWebSocketTransport.Reconnect(fakeConnection, null);

            Assert.Equal(1, fakeWebSocketTransport.GetInvocations("OpenWebSocket").Count());
            Assert.Equal(0, fakeConnection.GetInvocations("Stop").Count());
            Assert.Equal(0, fakeConnection.GetInvocations("OnError").Count());
        }

        [Fact]
        public void LostConnectionValidateArguments()
        {
            Assert.Equal("connection",
                Assert.Throws<ArgumentNullException>(
                    () => new WebSocketTransport().LostConnection(null)).ParamName);
        }

        [Fact]
        public void ExceptionCaughtAndReportedIfThrownWhenReadingMessage()
        {
            var exception = new Exception();
            var fakeDataReader = new FakeDataReader();

            fakeDataReader.Setup<string>("ReadString", () => { throw exception; });

            var fakeWebSocketResponse = new FakeWebSocketResponse();
            fakeWebSocketResponse.Setup("GetDataReader", () => fakeDataReader);

            var transport = new WebSocketTransport();
            transport.Start(
                new FakeConnection
                {
                    TransportConnectTimeout = new TimeSpan(0, 0, 0, 0, 100)
                },
                string.Empty, CancellationToken.None);

            var fakeConnection = new FakeConnection();
            transport.MessageReceived(fakeWebSocketResponse, fakeConnection);

            fakeConnection.Verify("OnError", new List<object[]> { new object[] { exception } });
        }

        [Fact]
        public void ExceptionCaughtAndReportedIfThrownWhenGettingDataReader()
        {
            var exception = new Exception();

            var fakeWebSocketResponse = new FakeWebSocketResponse();
            fakeWebSocketResponse.Setup<IDataReader>("GetDataReader", () => { throw exception; });

            var transport = new WebSocketTransport();
            transport.Start(
                new FakeConnection
                {
                    TransportConnectTimeout = new TimeSpan(0, 0, 0, 0, 100)
                },
                string.Empty, CancellationToken.None);

            var fakeConnection = new FakeConnection();
            transport.MessageReceived(fakeWebSocketResponse, fakeConnection);

            fakeConnection.Verify("OnError", new List<object[]> { new object[] { exception } });
        }
    }
}