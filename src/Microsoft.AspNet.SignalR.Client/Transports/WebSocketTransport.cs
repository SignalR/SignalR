// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD2_0

using System;
using System.IO;
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
        private IConnection _connection;
        private string _connectionData;
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

        protected override void OnStart(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            _disconnectToken = disconnectToken;
            _connection = connection;
            _connectionData = connectionData;

            // We don't need to await this task
            ConnectAndHandleConnection().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    TryFailStart(task.Exception);
                }
                else if (task.IsCanceled)
                {
                    TryFailStart(null);
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);
        }

        private async Task ConnectAndHandleConnection()
        {
            await PerformConnect(_disconnectToken);
            var linkedToken = CreateLinkedCancellationToken();
            await _webSocketHandler.ProcessWebSocketRequestAsync(_webSocket, linkedToken);
        }

        // For testing
        public virtual Task PerformConnect(CancellationToken token)
        {
            return PerformConnect(UrlBuilder.BuildConnect(_connection, Name, _connectionData), token);
        }

        private CancellationToken CreateLinkedCancellationToken()
        {
            // TODO: Revisit thread safety of this assignment
            _webSocketTokenSource = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_webSocketTokenSource.Token, _disconnectToken);
            return linkedCts.Token;
        }

        private async Task PerformConnect(string url, CancellationToken token)
        {
            var uri = UrlBuilder.ConvertToWebSocketUri(url);

            _connection.Trace(TraceLevels.Events, "WS Connecting to: {0}", uri);

            _webSocket = new ClientWebSocket();

            _connection.PrepareRequest(new WebSocketWrapperRequest(_webSocket, _connection));

            await _webSocket.ConnectAsync(uri, token);
        }

        protected override void OnStartFailed()
        {
            // if the transport failed to start we want to stop it silently.
            Dispose();
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
            _connection.Trace(TraceLevels.Messages, "WS: OnMessage({0})", message);

            ProcessResponse(_connection, message);
        }

        // virtual for testing
        internal virtual void OnOpen()
        {
            // This will noop if we're not in the reconnecting state
            if (_connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
            {
                _connection.OnReconnected();
            }
        }

        // virtual for testing
        internal virtual void OnClose()
        {
            _connection.Trace(TraceLevels.Events, "WS: OnClose()");

            // Make sure to try to fail start even if an abort has started.
            var startFailed = TryFailStart(new IOException(Resources.Error_TransportDisconnectedBeforeConnectionFullyInitialized));

            if (!AbortHandler.TryCompleteAbort() && !_disconnectToken.IsCancellationRequested && !startFailed)
            {
                _ = DoReconnect();
            }
        }

        // fire and forget
        private async Task DoReconnect()
        {
            try
            {
                var reconnectUrl = UrlBuilder.BuildReconnect(_connection, Name, _connectionData);

                while (TransportHelper.VerifyLastActive(_connection) && _connection.EnsureReconnecting())
                {
                    try
                    {
                        await PerformConnect(reconnectUrl, _disconnectToken);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (ExceptionHelper.IsRequestAborted(ex))
                        {
                            return;
                        }

                        _connection.OnError(ex);
                    }

                    await Task.Delay(ReconnectDelay);
                }

                var linkedToken = CreateLinkedCancellationToken();

                try
                {
                    await _webSocketHandler.ProcessWebSocketRequestAsync(_webSocket, linkedToken);
                }
                catch
                {
                    // Ignore any errors from ProcessWebSocketRequestAsync just as OnStart does after the init message is received.
                    // Any errors other than one thrown from the final CloseAsync is reported via OnError(Exception).
                }
            }
            catch (Exception ex)
            {
                _connection.Trace(TraceLevels.Events, "WS DoReconnect() failed: {0}", ex);
            }
        }

        // virtual for testing
        internal virtual void OnError(Exception error)
        {
            _connection.OnError(error);
        }

        public override void LostConnection(IConnection connection)
        {
            _connection.Trace(TraceLevels.Events, "WS: LostConnection");

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
    }
}

#elif NET40 || NETSTANDARD1_3
// Not supported on this framework.
#else 
#error Unsupported target framework.
#endif
