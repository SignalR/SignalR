// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
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
        private CancellationToken _disconnectToken;
        private WebSocketConnectionInfo _connectionInfo;
        private TaskCompletionSource<object> _startTcs;

        public WebSocketTransport()
            : this(new DefaultHttpClient())
        {
        }

        public WebSocketTransport(IHttpClient client)
        {
            _client = client;
            _disconnectToken = CancellationToken.None;

            ReconnectDelay = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

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

            return _startTcs.Task;
        }

        private async Task PerformConnect(bool reconnecting = false)
        {
            var url = reconnecting ? _connectionInfo.Connection.Url : _connectionInfo.Connection.Url + "connect";
            url += TransportHelper.GetReceiveQueryString(_connectionInfo.Connection, _connectionInfo.Data, "webSockets");
            var builder = new UriBuilder(url);
            builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";

            Debug.WriteLine("WS: " + builder.Uri);

            var webSocket = new ClientWebSocket();
            _connectionInfo.Connection.PrepareRequest(new WebSocketWrapperRequest(webSocket));

            await webSocket.ConnectAsync(builder.Uri, _disconnectToken);
            await ProcessWebSocketRequestAsync(webSocket, _disconnectToken);
        }

        public void Abort(IConnection connection)
        {
            Close();
        }

        public Task Send(IConnection connection, string data)
        {
            return SendAsync(data);
        }

        public override void OnMessage(string message)
        {
            Debug.WriteLine("WS Receive: " + message);

            bool timedOut;
            bool disconnected;
            TransportHelper.ProcessResponse(_connectionInfo.Connection,
                                            message,
                                            out timedOut,
                                            out disconnected);

            if (disconnected && !_disconnectToken.IsCancellationRequested)
            {
                _connectionInfo.Connection.Disconnect();
                Close();
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

        public override void OnClose(bool clean)
        {
            if (_disconnectToken.IsCancellationRequested)
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
            Debug.WriteLine("OnError({0}, {1})", _connectionInfo.Connection.ConnectionId, Error);

            _connectionInfo.Connection.OnError(Error);
        }

        private class WebSocketConnectionInfo
        {
            public IConnection Connection { get; private set; }
            public string Data { get; private set; }

            public WebSocketConnectionInfo(IConnection connection, string data)
            {
                Connection = connection;
                Data = data;
            }
        }
    }
}
