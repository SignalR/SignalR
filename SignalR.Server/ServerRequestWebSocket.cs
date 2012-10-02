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

        // Queue for sending messages
        private readonly TaskQueue _sendQueue = new TaskQueue(); 

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

            // Invoke the call back as we're ready to start receiving
            Task task = _callback(this)
                .Then(cts => cts.Cancel(false), _cts)
                .Catch(ex => _cts.Cancel(false));

            StartReceiving();

            return task;
        }

        public Action<string> OnMessage { get; set; }
        public Action OnClose { get; set; }
        public Action OnUngracefulClose { get; set; }
        public Action<Exception> OnError { get; set; }

        public Task Send(string value)
        {
            // REVIEW: Should we return this task?
            _sendQueue.Enqueue(() =>
            {
                var data = Encoding.UTF8.GetBytes(value);
                return _sendAsync(new ArraySegment<byte>(data), (int)WebSocketMessageType.Text, true, CancellationToken.None);
            });

            return TaskAsyncHelper.Empty;
        }

        private void StartReceiving()
        {
            ContinueReceiving(receiveData: null);
        }

        private void ContinueReceiving(WebSocketReceiveTuple receiveData)
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (receiveData != null)
                    {
                        goto process;
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

                process:
                    var messageType = (WebSocketMessageType)receiveData.Item1;
                    bool endOfMessage = receiveData.Item2;
                    int? count = receiveData.Item3;

                    receiveData = null;

                    if (messageType == WebSocketMessageType.Text)
                    {
                        if (count.HasValue)
                        {
                            var text = Encoding.UTF8.GetString(_buffer.Array, 0, count.Value);
                            if (_text != null)
                            {
                                _text += text;
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

                    if (messageType == WebSocketMessageType.Close)
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

            if (OnUngracefulClose != null)
            {
                try
                {
                    OnUngracefulClose();
                }
                catch
                {

                }
            }
        }

        // IANA has added initial values to the registry as follows.
        // |Opcode  | Meaning                             | Reference |
        //-+--------+-------------------------------------+-----------|
        // | 0      | Continuation Frame                  | RFC 6455 <http://tools.ietf.org/html/rfc6455>   |
        //-+--------+-------------------------------------+-----------|
        // | 1      | Text Frame                          | RFC 6455 <http://tools.ietf.org/html/rfc6455>   |
        //-+--------+-------------------------------------+-----------|
        // | 2      | Binary Frame                        | RFC 6455 <http://tools.ietf.org/html/rfc6455>   |
        //-+--------+-------------------------------------+-----------|
        // | 8      | Connection Close Frame              | RFC 6455 <http://tools.ietf.org/html/rfc6455>   |
        //-+--------+-------------------------------------+-----------|
        // | 9      | Ping Frame                          | RFC 6455 <http://tools.ietf.org/html/rfc6455>   |
        //-+--------+-------------------------------------+-----------|
        // | 10     | Pong Frame                          | RFC 6455 <http://tools.ietf.org/html/rfc6455>   |
        //-+--------+-------------------------------------+-----------|
        private enum WebSocketMessageType
        {
            Continuation = 0,
            Text = 1,
            Binary = 2,
            Close = 8,
            Ping = 9,
            Pong = 10
        }
    }
}
