// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Transports.WebSockets;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : ClientTransportBase
    {
        private readonly ClientWebSocketHandler _webSocketHandler;
        private CancellationToken _disconnectToken;
        private TransportInitializationHandler _initializeHandler;
        private WebSocketConnectionInfo _connectionInfo;
        private CancellationTokenSource _webSocketTokenSource;
        private ClientWebSocket _webSocket;
        private int _disposed;

        public WebSocketTransport()
            : this(new DefaultHttpClient())
        {
        }

        public WebSocketTransport(IHttpClient client)
            : base(client, "webSockets")
        {
            _disconnectToken = CancellationToken.None;
            ReconnectDelay = TimeSpan.FromSeconds(2);
            _webSocketHandler = new ClientWebSocketHandler(this);
        }

        // intended for testing
        internal WebSocketTransport(ClientWebSocketHandler webSocketHandler)
            : this()
        {
            _webSocketHandler = webSocketHandler;
        }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        /// <summary>
        /// Indicates whether or not the transport supports keep alive
        /// </summary>
        public override bool SupportsKeepAlive
        {
            get { return true; }
        }

        public override Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            _initializeHandler = new TransportInitializationHandler(HttpClient, connection, connectionData, Name, disconnectToken, TransportHelper);

            // Tie into the OnFailure event so that we can stop the transport silently.
            _initializeHandler.OnFailure += () =>
            {
                Dispose();
            };

            _disconnectToken = disconnectToken;
            _connectionInfo = new WebSocketConnectionInfo(connection, connectionData);

            // We don't need to await this task
            PerformConnect().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _initializeHandler.Fail(task.Exception);
                }
                else if (task.IsCanceled)
                {
                    _initializeHandler.Fail();
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);

            return _initializeHandler.Task;
        }

        // For testing
        public virtual Task PerformConnect()
        {
            return PerformConnect(reconnecting: false);
        }

        private async Task PerformConnect(bool reconnecting)
        {
            var urlBuilder = new UrlBuilder();

            var url = reconnecting
                ? urlBuilder.BuildReconnect(_connectionInfo.Connection, Name, _connectionInfo.Data)
                : urlBuilder.BuildConnect(_connectionInfo.Connection, Name, _connectionInfo.Data);

            var builder = new UriBuilder(url);
            builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";

            _connectionInfo.Connection.Trace(TraceLevels.Events, "WS Connecting to: {0}", builder.Uri);
 
            // TODO: Revisit thread safety of this assignment
            _webSocketTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            _connectionInfo.Connection.PrepareRequest(new WebSocketWrapperRequest(_webSocket, _connectionInfo.Connection));

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_webSocketTokenSource.Token, _disconnectToken);
            CancellationToken token = linkedCts.Token;

            await _webSocket.ConnectAsync(builder.Uri, token);
            await _webSocketHandler.ProcessWebSocketRequestAsync(_webSocket, token);
        }

        public override Task Send(IConnection connection, string data, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            // If we don't throw here when the WebSocket isn't open, WebSocketHander.SendAsync will noop.
            if (_webSocketHandler.WebSocket.State != WebSocketState.Open)
            {
                // Make this a faulted task and trigger the OnError even to maintain consistency with the HttpBasedTransports
                var ex = new InvalidOperationException(Resources.Error_DataCannotBeSentDuringWebSocketReconnect);
                connection.OnError(ex);
                return TaskAsyncHelper.FromError(ex);
            }

            return _webSocketHandler.SendAsync(data);
        }

        // virtual for testing
        internal virtual void OnMessage(string message)
        {
            _connectionInfo.Connection.Trace(TraceLevels.Messages, "WS: OnMessage({0})", message);

            bool timedOut;
            bool disconnected;
            TransportHelper.ProcessResponse(_connectionInfo.Connection,
                                            message,
                                            out timedOut,
                                            out disconnected,
                                            _initializeHandler.InitReceived);

            if (disconnected && !_disconnectToken.IsCancellationRequested)
            {
                _connectionInfo.Connection.Trace(TraceLevels.Messages, "Disconnect command received from server.");
                _connectionInfo.Connection.Disconnect();
            }
        }

        // virtual for testing
        internal virtual void OnOpen()
        {
            // This will noop if we're not in the reconnecting state
            if (_connectionInfo.Connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
            {
                _connectionInfo.Connection.OnReconnected();
            }
        }

        // virtual for testing
        internal virtual void OnClose()
        {
            _connectionInfo.Connection.Trace(TraceLevels.Events, "WS: OnClose()");

            if (_disconnectToken.IsCancellationRequested)
            {
                return;
            }

            if (AbortHandler.TryCompleteAbort())
            {
                return;
            }

            DoReconnect();
        }

        private async void DoReconnect()
        {
            while (TransportHelper.VerifyLastActive(_connectionInfo.Connection) && _connectionInfo.Connection.EnsureReconnecting())
            {
                try
                {
                    await PerformConnect(reconnecting: true);
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (ExceptionHelper.IsRequestAborted(ex))
                    {
                        break;
                    }

                    _connectionInfo.Connection.OnError(ex);
                }

                await Task.Delay(ReconnectDelay);
            }
        }

        // virtual for testing
        internal virtual void OnError(Exception error)
        {
            _connectionInfo.Connection.OnError(error);
        }

        public override void LostConnection(IConnection connection)
        {
            _connectionInfo.Connection.Trace(TraceLevels.Events, "WS: LostConnection");

            if (_webSocketTokenSource != null)
            {
                _webSocketTokenSource.Cancel();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1)
                {
                    base.Dispose(disposing);
                    return;
                }

                if (_webSocketTokenSource != null)
                {
                    // Gracefully close the websocket message loop
                    _webSocketTokenSource.Cancel();
                }

                if (_webSocket != null)
                {
                    _webSocket.Dispose();
                }

                if (_webSocketTokenSource != null)
                {
                    _webSocketTokenSource.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "This class is just a data holder")]
        private class WebSocketConnectionInfo
        {
            public IConnection Connection;
            public string Data;

            public WebSocketConnectionInfo(IConnection connection, string data)
            {
                Connection = connection;
                Data = data;
            }
        }
    }
}
