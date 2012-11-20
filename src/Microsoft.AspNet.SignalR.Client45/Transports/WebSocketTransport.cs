using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.WebSockets;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : WebSocketHandler, IClientTransport
    {
        private readonly IHttpClient _client;
        private volatile bool _stop;
        private WebSocketConnectionInfo _connectionInfo;
        private TaskCompletionSource<object> _startTcs;

        public WebSocketTransport(IHttpClient client)
        {
            _client = client;

            ReconnectDelay = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return TransportHelper.GetNegotiationResponse(_client, connection);
        }

        public Task Start(IConnection connection, string data)
        {
            _startTcs = new TaskCompletionSource<object>();

            _connectionInfo = new WebSocketConnectionInfo(connection, data);

            // We don't need to await this task
            Task task = PerformConnect(connection, data);

            return _startTcs.Task;
        }

        private async Task PerformConnect(IConnection connection, string data, bool reconnecting = false)
        {
            var url = reconnecting ? connection.Url : connection.Url + "/connect";
            url += TransportHelper.GetReceiveQueryString(connection, data, "webSockets");
            var builder = new UriBuilder(url);
            builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";

            Debug.WriteLine("WS: " + builder.Uri);

            var webSocket = new ClientWebSocket();

            await webSocket.ConnectAsync(builder.Uri, CancellationToken.None);
            await ProcessWebSocketRequestAsync(webSocket);
        }

        public void Stop(IConnection connection)
        {
            _stop = true;

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

            if (disconnected && !_stop)
            {
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
            if (_stop)
            {
                return;
            }

            if (_connectionInfo.Connection.ChangeState(ConnectionState.Connected, ConnectionState.Reconnecting))
            {
                DoReconnect();
            }
        }

        private async void DoReconnect()
        {
            while (true)
            {
                try
                {
                    await PerformConnect(_connectionInfo.Connection, _connectionInfo.Data, reconnecting: true);
                    break;
                }
                catch(Exception ex)
                {
                    _connectionInfo.Connection.OnError(ex);
                }

                await Task.Delay(ReconnectDelay);
            }
        }

        public override void OnError()
        {
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
