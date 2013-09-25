// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.WebSockets;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : WebSocketHandler, IClientTransport
    {
        private readonly IHttpClient _client;
        private readonly TransportAbortHandler _abortHandler;
        private CancellationToken _disconnectToken;
        private WebSocketConnectionInfo _connectionInfo;
        private TaskCompletionSource<object> _startTcs;
        private CancellationTokenSource _webSocketTokenSource;
        private ClientWebSocket _webSocket;
        private int _disposed;

        public WebSocketTransport()
            : this(new DefaultHttpClient())
        {
        }

        public WebSocketTransport(IHttpClient client)
            : base(maxIncomingMessageSize: null) // Disable max incoming message size on the client
        {
            _client = client;
            _disconnectToken = CancellationToken.None;
            _abortHandler = new TransportAbortHandler(client, Name);
            ReconnectDelay = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        /// <summary>
        /// Indicates whether or not the transport supports keep alive
        /// </summary>
        public bool SupportsKeepAlive
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The name of the transport.
        /// </summary>
        public string Name
        {
            get
            {
                return "webSockets";
            }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return _client.GetNegotiationResponse(connection);
        }

        public Task Start(IConnection connection, string data, CancellationToken disconnectToken)
        {
            _startTcs = new TaskCompletionSource<object>();
            _disconnectToken = disconnectToken;
            _connectionInfo = new WebSocketConnectionInfo(connection, data);

            // We don't need to await this task
            PerformConnect().ContinueWithNotComplete(_startTcs);

            _startTcs.Task.ContinueWith(_ => Dispose(), TaskContinuationOptions.NotOnRanToCompletion);
            
            return _startTcs.Task;
        }

        private async Task PerformConnect(bool reconnecting = false)
        {
            var url = _connectionInfo.Connection.Url + (reconnecting ? "reconnect" : "connect");
            url += TransportHelper.GetReceiveQueryString(_connectionInfo.Connection, _connectionInfo.Data, "webSockets");
            var builder = new UriBuilder(url);
            builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";

            _connectionInfo.Connection.Trace(TraceLevels.Events, "WS: {0}", builder.Uri);
 
            // TODO: Revisit thread safety of this assignment
            _webSocketTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            _connectionInfo.Connection.PrepareRequest(new WebSocketWrapperRequest(_webSocket));

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_webSocketTokenSource.Token, _disconnectToken);
            CancellationToken token = linkedCts.Token;

            await _webSocket.ConnectAsync(builder.Uri, token);
            await ProcessWebSocketRequestAsync(_webSocket, token);
        }

        public void Abort(IConnection connection, TimeSpan timeout)
        {
            _abortHandler.Abort(connection, timeout);
        }

        public Task Send(IConnection connection, string data)
        {
            return SendAsync(data);
        }

        public override void OnMessage(string message)
        {
            _connectionInfo.Connection.Trace(TraceLevels.Messages, "WS: OnMessage({0})", message);

            bool timedOut;
            bool disconnected;
            TransportHelper.ProcessResponse(_connectionInfo.Connection,
                                            message,
                                            out timedOut,
                                            out disconnected);

            if (disconnected && !_disconnectToken.IsCancellationRequested)
            {
                _connectionInfo.Connection.Disconnect();
            }
        }

        public override void OnOpen()
        {
            if (!_startTcs.TrySetResult(null) &&
                _connectionInfo.Connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
            {
                _connectionInfo.Connection.OnReconnected();
            }
        }

        public override void OnClose()
        {
            _connectionInfo.Connection.Trace(TraceLevels.Events, "WS: OnClose()");

            if (_disconnectToken.IsCancellationRequested)
            {
                return;
            }

            if (_abortHandler.TryCompleteAbort())
            {
                return;
            }

            DoReconnect();
        }

        private async void DoReconnect()
        {
            while (_connectionInfo.Connection.EnsureReconnecting())
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

        public override void OnError()
        {
            _connectionInfo.Connection.OnError(Error);
        }

        public void LostConnection(IConnection connection)
        {
            _connectionInfo.Connection.Trace(TraceLevels.Events, "WS: LostConnection");

            if (_webSocketTokenSource != null)
            {
                _webSocketTokenSource.Cancel();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1)
                {
                    return;
                }

                if (_webSocketTokenSource != null)
                {
                    // Gracefully close the websocket message loop
                    _webSocketTokenSource.Cancel();
                }

                _abortHandler.Dispose();
                
                if (_webSocket != null)
                {
                    _webSocket.Dispose();
                }

                if (_webSocketTokenSource != null)
                {
                    _webSocketTokenSource.Dispose();
                }
            }
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
