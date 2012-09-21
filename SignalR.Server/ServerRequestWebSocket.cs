using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Server
{
    using WebSocketReceiveAsync =
        Func
        <
            ArraySegment<byte> /* data */,
            CancellationToken /* cancel */,
            Task
            <
                Tuple
                <
                    int /* messageType */,
                    bool /* endOfMessage */,
                    int? /* count */,
                    int? /* closeStatus */,
                    string /* closeStatusDescription */
                >
            >
        >;
    using WebSocketReceiveTuple =
            Tuple
            <
                int /* messageType */,
                bool /* endOfMessage */,
                int? /* count */,
                int? /* closeStatus */,
                string /* closeStatusDescription */
            >;
    using WebSocketSendAsync =
           Func
           <
               ArraySegment<byte> /* data */,
               int /* messageType */,
               bool /* endOfMessage */,
               CancellationToken /* cancel */,
               Task
           >;

    internal class ServerRequestWebSocket : IWebSocket
    {
        private readonly Func<IWebSocket, Task> _callback;
        private readonly TaskQueue _sendQueue = new TaskQueue(); // queue for sending messages

        private WebSocketSendAsync _sendAsync;
        private WebSocketReceiveAsync _receiveAsync;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ArraySegment<byte> _buffer = new ArraySegment<byte>(new byte[8 << 10]);
        private string _text;

        public ServerRequestWebSocket(Func<IWebSocket, Task> callback)
        {
            _callback = callback;
        }

        public Task Invoke(IDictionary<string, object> context)
        {
            _sendAsync = (WebSocketSendAsync)context["websocket.SendAsyncFunc"];
            _receiveAsync = (WebSocketReceiveAsync)context["websocket.ReceiveAsyncFunc"];

            var task = _callback(this)
                .Then(cts => cts.Cancel(false), _cts)
                .Catch(ex => _cts.Cancel(false));

            StartReceiving();
            return task;
        }

        public Action<string> OnMessage { get; set; }
        public Action OnClose { get; set; }
        public Action<Exception> OnError { get; set; }

        public Task Send(string value)
        {
            _sendQueue.Enqueue(
                () =>
                {
                    var data = Encoding.UTF8.GetBytes(value);
                    return _sendAsync(new ArraySegment<byte>(data), 1, true, CancellationToken.None);
                });
            return TaskAsyncHelper.Empty;
        }

        void StartReceiving()
        {
            ContinueReceiving(null);
        }

        private void ContinueReceiving(WebSocketReceiveTuple receiveData)
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (receiveData != null)
                    {
                        goto mark1;
                    }

                    var receiveTask = _receiveAsync(_buffer, _cts.Token);
                    if (!receiveTask.IsCompleted)
                    {
                        receiveTask
                            .Then((Action<WebSocketReceiveTuple>)ContinueReceiving)
                            .Catch(ErrorReceiving);
                        return;
                    }
                    receiveData = receiveTask.Result;

                mark1:
                    var messageType = receiveData.Item1;
                    var endOfMessage = receiveData.Item2;
                    var count = receiveData.Item3;
                    receiveData = null;
                    if (messageType == 1)
                    {
                        if (count.HasValue)
                        {
                            var text = Encoding.UTF8.GetString(_buffer.Array, 0, count.Value);
                            if (_text != null)
                            {
                                _text = _text + text;
                            }
                            else
                            {
                                _text = text;
                            }
                        }
                        if (endOfMessage)
                        {
                            var text = _text;
                            _text = null;
                            if (OnMessage != null)
                            {
                                OnMessage(text);
                            }
                        }
                        continue;
                    }

                    if (messageType == 8)
                    {
                        if (OnClose != null)
                        {
                            OnClose();
                        }
                        return;
                    }

                    throw new InvalidOperationException("Unexpected websocket message type");
                }
            }
            catch (Exception ex)
            {
                ErrorReceiving(ex);
            }
        }

        private void ErrorReceiving(Exception error)
        {
            if (OnError != null)
            {
                try
                {
                    OnError(error);
                }
                catch
                {
                }
            }
            if (OnClose != null)
            {
                try
                {
                    OnClose();
                }
                catch
                {
                }
            }
        }
    }
}
