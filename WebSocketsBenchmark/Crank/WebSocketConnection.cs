using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Crank
{
    class WebSocketConnection : IDisposable
    {
        private readonly ClientWebSocket _client;
        private readonly CancellationTokenSource _source;
        private readonly string _url;
        private Task _readTask;
        public event Action Closed;

        public WebSocketConnection(string url)
        {
            _client = new ClientWebSocket();
            _source = new CancellationTokenSource();
            _url = url;
            State = ConnectionState.Disconnected;
        }

        public async Task Start(IClientTransport transport)
        {
            await Start();
        }

        public async Task Start()
        {
            State = ConnectionState.Connecting;
            await _client.ConnectAsync(new Uri(_url), CancellationToken.None);
            State = ConnectionState.Connected;
            _readTask = Task.Factory.StartNew(
                async () =>
                {
                    var buffer = new ArraySegment<Byte>(new byte[1024]);
                    while (!_source.Token.IsCancellationRequested)
                    {
                        await _client.ReceiveAsync(buffer, _source.Token);
                    }
                });
        }

        public ConnectionState State { get; private set; }

        public async Task Send(string message)
        {
            var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
            await Send(buffer);
        }

        public async Task Send(ArraySegment<byte> buffer)
        {
            try
            {
                await _client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception) { }
        }

        public void Dispose()
        {
            if (State != ConnectionState.Disconnected)
            {
                State = ConnectionState.Disconnected;
                _source.Cancel();
                _readTask.Wait();
                _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None).Wait();

                var closed = Closed; if (closed != null) { closed(); }
            }

            _client.Dispose();
        }
    }
}
