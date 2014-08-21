// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
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
        private TransportInitializationHandler _initializationHandler;

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

        public override Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");    
            }

            _connection = connection;
            _connectionData = connectionData;

            _initializationHandler = new TransportInitializationHandler(HttpClient, connection, connectionData, Name, disconnectToken, TransportHelper);
            _initializationHandler.OnFailure += DisposeSocket;

            return Start(connection, connectionData, _initializationHandler);
        }

        // testing - allows injecting custom initializationHandler
        internal Task Start(IConnection connection, string connectionData, TransportInitializationHandler initializationHandler)
        {
            Task.Run(async () =>
            {
                try
                {
                    await StartWebSocket(connection, UrlBuilder.BuildConnect(connection, Name, connectionData));
                }
                catch (TaskCanceledException)
                {
                    initializationHandler.Fail();
                }
                catch (Exception ex)
                {
                    initializationHandler.Fail(ex);
                }
            });

            return initializationHandler.Task;
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
            MessageReceived(new MessageReceivedEventArgsWrapper(eventArgs), _connection, _initializationHandler);
        }

        // internal for testing, passing dependencies to allow mocking
        internal void MessageReceived(IWebSocketResponse webSocketResponse, IConnection connection, 
            TransportInitializationHandler initializationHandler)
        {
            var response = ReadMessage(webSocketResponse);

            connection.Trace(TraceLevels.Messages, "WS: OnMessage({0})", response);

            ProcessResponse(connection, response, initializationHandler.InitReceived);
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
                throw new InvalidOperationException(Resources.GetResourceString("Error_WebSocketUninitialized"));
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

            if (AbortHandler.TryCompleteAbort())
            {
                return;
            }

            Task.Run(() => Reconnect(_connection, _connectionData));
        }

        // internal for testing
        internal async Task Reconnect(IConnection connection, string connectionData)
        {
            var reconnectUrl = UrlBuilder.BuildReconnect(connection, Name, connectionData);

            while (TransportHelper.VerifyLastActive(connection) && connection.EnsureReconnecting())
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

            LostConnection(connection, _webSocket);

        }

        // internal for testing
        internal static void LostConnection(IConnection connection, IWebSocket webSocket)
        {
            connection.Trace(TraceLevels.Events, "WS: LostConnection");

            webSocket.Close(SuccessCloseStatus, string.Empty);            
        }

        private void DisposeSocket()
        {
            var webSocket = Interlocked.Exchange(ref _webSocket, null);
            if (webSocket != null)
            {
                webSocket.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeSocket();                
            }

            base.Dispose(disposing);
        }
    }
}
