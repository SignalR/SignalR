// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Windows.Networking.Sockets;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Windows.Storage.Streams;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : ClientTransportBase
    {
        private const ushort SuccessCloseStatus = 1000;

        private IConnection _connection;
        private string _connectionData;
        private MessageWebSocket _webSocket;
        private CancellationToken _disconnectToken;

        public WebSocketTransport()
            : this(new DefaultHttpClient())
        {
        }

        public WebSocketTransport(IHttpClient httpClient)
            : base(httpClient, "webSockets")
        {
        }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        public override bool SupportsKeepAlive
        {
            get { return true; }
        }

        protected override void OnStart(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            _connection = connection;
            _connectionData = connectionData;
            _disconnectToken = disconnectToken;

            Task.Run(() => Start(connection, connectionData));
        }

        private async Task Start(IConnection connection, string connectionData)
        {
            try
            {
                await StartWebSocket(connection, UrlBuilder.BuildConnect(connection, Name, connectionData));
            }
            catch (TaskCanceledException)
            {
                TransportFailed(null);
            }
            catch (Exception ex)
            {
                TransportFailed(ex);
            }
        }

        protected override void OnStartFailed()
        {
            // if the transport failed to start we want to stop it silently.
            Dispose();
        }

        private async Task StartWebSocket(IConnection connection, string url)
        {
            var uri = UrlBuilder.ConvertToWebSocketUri(url);
            connection.Trace(TraceLevels.Events, "WS Connecting to: {0}", uri);

            if (_webSocket == null)
            {
                var webSocket = new MessageWebSocket();
                webSocket.Control.MessageType = SocketMessageType.Utf8;
                webSocket.Closed += WebsocketClosed;
                webSocket.MessageReceived += MessageReceived;
                connection.PrepareRequest(new WebSocketRequest(webSocket));
                await OpenWebSocket(webSocket, uri);
                _webSocket = webSocket;
            }
         }

        // testing/mocking
        protected virtual async Task OpenWebSocket(IWebSocket webSocket, Uri uri)
        {
            await webSocket.ConnectAsync(uri);
        }

        private void MessageReceived(MessageWebSocket webSocket, MessageWebSocketMessageReceivedEventArgs eventArgs)
        {
            MessageReceived(new MessageReceivedEventArgsWrapper(eventArgs), _connection);
        }

        // internal for testing, passing dependencies to allow mocking
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", 
            Justification = "Exceptions are reported to the user using IConnection.Error")]
        internal void MessageReceived(IWebSocketResponse webSocketResponse, IConnection connection)
        {
            string response;

            try
            {
                response = ReadMessage(webSocketResponse);
            }
            catch (Exception ex)
            {
                connection.OnError(ex);

                var webSocket = _webSocket;
                if (webSocket != null)
                {
                    webSocket.Close(SuccessCloseStatus, ex.Message);
                }

                return;
            }

            connection.Trace(TraceLevels.Messages, "WS: OnMessage({0})", response);

            ProcessResponse(connection, response);
        }

        private static string ReadMessage(IWebSocketResponse webSocketResponse)
        {
            var reader = webSocketResponse.GetDataReader();
            using ((IDisposable)reader)
            {
                reader.UnicodeEncoding = UnicodeEncoding.Utf8;
                return reader.ReadString(reader.UnconsumedBufferLength);
            }
        }

        public override Task Send(IConnection connection, string data, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var webSocket = _webSocket;

            if (webSocket == null)
            {
                Exception ex;
                if (connection.State != ConnectionState.Disconnected)
                {
                    // Make this a faulted task and trigger the OnError even to maintain consistency with the HttpBasedTransports
                    ex = new InvalidOperationException(Resources.GetResourceString("Error_DataCannotBeSentDuringWebSocketReconnect"));
                    connection.OnError(ex);
                }
                else
                {
                    ex = new InvalidOperationException(Resources.GetResourceString("Error_WebSocketUninitialized"));
                }

                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(ex);
                return tcs.Task;
            }

            return Send(webSocket, data);
        }

        // internal for testing
        internal static async Task Send(IWebSocket webSocket, string data)
        {
            using (var messageWriter = new DataWriter(webSocket.OutputStream))
            {
                messageWriter.WriteString(data);
                await messageWriter.StoreAsync();
                messageWriter.DetachStream();
            }
        }
        
        private void WebsocketClosed(IWebSocket webSocket, WebSocketClosedEventArgs eventArgs)
        {
            _connection.Trace(TraceLevels.Events, "WS: WebsocketClosed - Code: {0}, Reason {1}", eventArgs.Code, eventArgs.Reason);

            DisposeSocket();

            if (AbortHandler.TryCompleteAbort() || _disconnectToken.IsCancellationRequested)
            {
                return;
            }

            Task.Run(() => Reconnect(_connection, _connectionData));
        }

        // internal for testing
        internal async Task Reconnect(IConnection connection, string connectionData)
        {
            var reconnectUrl = UrlBuilder.BuildReconnect(connection, Name, connectionData);

            while (TransportHelper.VerifyLastActive(connection) && connection.EnsureReconnecting() && !_disconnectToken.IsCancellationRequested)
            {
                try
                {
                    await StartWebSocket(connection, reconnectUrl);

                    if (connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
                    {
                        connection.OnReconnected();
                    }

                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    connection.OnError(ex);
                }

                await Task.Delay(ReconnectDelay);
            }
        }

        public override void LostConnection(IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            connection.Trace(TraceLevels.Events, "WS: LostConnection");

            DisposeSocket();
        }

        private void DisposeSocket()
        {
            var webSocket = Interlocked.Exchange(ref _webSocket, null);
            if (webSocket != null)
            {
                // this is a no-op if the socket is not open
                webSocket.Close(SuccessCloseStatus, string.Empty);
                webSocket.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            // we call base.Dispose() first because it will dispose abort handler which will prevent from 
            // reconnection attempts when the websoscket is close as a result of disposing of the websocket below
            base.Dispose(disposing);

            if (disposing)
            {
                DisposeSocket();
            }
        }
    }
}
