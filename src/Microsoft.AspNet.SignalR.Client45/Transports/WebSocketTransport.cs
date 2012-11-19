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
        private bool _stop;

        public WebSocketTransport(IHttpClient client)
        {
            _client = client;
        }

        private WebSocketConnectionInfo ConnectionInfo { get; set; }

        private TaskCompletionSource<object> StartTcs { get; set; }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return TransportHelper.GetNegotiationResponse(_client, connection);
        }

        public Task Start(IConnection connection, string data)
        {
            StartTcs = new TaskCompletionSource<object>();

            ConnectionInfo = new WebSocketConnectionInfo(connection, data);

            DoConnect(connection, data, ex => StartTcs.TrySetException(ex));

            return StartTcs.Task;
        }

        private void DoConnect(IConnection connection, string data, Action<Exception> errorCallback, bool reconnecting = false)
        {
            var url = reconnecting ? connection.Url : connection.Url + "/connect";
            url += TransportHelper.GetReceiveQueryString(connection, data, "webSockets");
            var builder = new UriBuilder(url);
            builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";

            Debug.WriteLine("WS: " + builder.Uri);

            var webSocket = new ClientWebSocket();

            webSocket.ConnectAsync(builder.Uri, CancellationToken.None)
                     .Catch(errorCallback)
                     .Then(ws => ProcessWebSocketRequestAsync(ws), webSocket);
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
            TransportHelper.ProcessResponse(ConnectionInfo.Connection, 
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
            if (!StartTcs.TrySetResult(null) && 
                ConnectionInfo.Connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
            {
                ConnectionInfo.Connection.OnReconnected();
            }
        }

        public override void OnClose(bool clean)
        {
            if (_stop)
            {
                return;
            }

            if (ConnectionInfo.Connection.ChangeState(ConnectionState.Connected, ConnectionState.Reconnecting))
            {
                DoConnect(ConnectionInfo.Connection,
                          ConnectionInfo.Data,
                          e => { },
                          reconnecting: true);
            }
        }

        public override void OnError()
        {
            ConnectionInfo.Connection.OnError(Error);
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
