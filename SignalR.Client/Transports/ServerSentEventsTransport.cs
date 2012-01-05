using System;
using System.IO;
using System.Net;
using System.Text;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        public ServerSentEventsTransport()
            : base("serverSentEvents")
        {
        }

        protected override void OnStart(Connection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            OpenConnection(connection, data, initializeCallback, errorCallback);
        }

        private void OpenConnection(Connection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            var url = connection.Url + GetReceiveQueryString(connection, data);

            Action<HttpWebRequest> prepareRequest = PrepareRequest(connection);

            HttpHelper.GetAsync(url, request =>
            {
                prepareRequest(request);

                request.Accept = "text/event-stream";

                if (connection.MessageId != null)
                {
                    request.Headers["Last-Event-ID"] = connection.MessageId.ToString();
                }

            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    errorCallback(task.Exception);
                }
                else
                {
                    // Get the reseponse stream and read it for messages
                    var stream = task.Result.GetResponseStream();
                    var handler = new AsyncStreamReader(stream,
                                                        connection,
                                                        initializeCallback,
                                                        exception => OpenConnection(connection, data, initializeCallback, errorCallback));
                    handler.StartReading();
                }
            });
        }

        private class AsyncStreamReader
        {
            private readonly Stream _stream;
            private readonly ChunkBuffer _buffer;
            private readonly Action _initializeCallback;
            private readonly Action<Exception> _errorCallback;
            private readonly Connection _connection;
            private int _processingQueue;
            private bool _reading;
            private bool _processingBuffer;

            public AsyncStreamReader(Stream stream, Connection connection, Action initializeCallback, Action<Exception> errorCallback)
            {
                _initializeCallback = initializeCallback;
                _errorCallback = errorCallback;
                _stream = stream;
                _connection = connection;
                _buffer = new ChunkBuffer();
            }

            public void StartReading()
            {
                _reading = true;
                ReadLoop();
            }

            public void StopReading()
            {
                _reading = false;
            }

            private void ReadLoop()
            {
                if (!_reading)
                {
                    return;
                }

                var buffer = new byte[1024];
                _stream.ReadAsync(buffer).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Exception exception = task.Exception.GetBaseException();

                        _connection.OnError(exception);

                        _errorCallback(exception);
                        return;
                    }

                    int read = task.Result;

                    if (read > 0)
                    {
                        // Put chunks in the buffer
                        _buffer.Add(buffer, read);
                    }

                    // Keep reading the next set of data
                    ReadLoop();

                    if (read <= buffer.Length)
                    {
                        // If we read less than we wanted or if we filled the buffer, process it
                        ProcessBuffer();
                    }
                });
            }

            private void ProcessBuffer()
            {
                if (_processingBuffer)
                {
                    // Increment the number of times we should process messages
                    _processingQueue++;
                    return;
                }

                _processingBuffer = true;

                int total = Math.Max(1, _processingQueue);

                for (int i = 0; i < total; i++)
                {
                    ProcessChunks();
                }

                _processingQueue -= total;

                _processingBuffer = false;
            }

            private void ProcessChunks()
            {
                while (_buffer.HasChunks)
                {
                    string line = _buffer.ReadLine();

                    // Stop when we read an empty line
                    if (String.IsNullOrEmpty(line))
                    {
                        break;
                    }

                    // Try parsing the sseEvent
                    SseEvent sseEvent;
                    if (!TryParseEvent(line, out sseEvent))
                    {
                        continue;
                    }

                    switch (sseEvent.Type)
                    {
                        case EventType.Id:
                            long id;
                            if (Int64.TryParse(sseEvent.Data, out id))
                            {
                                _connection.MessageId = id;
                            }
                            break;
                        case EventType.Data:
                            if (sseEvent.Data.Equals("initialized", StringComparison.OrdinalIgnoreCase))
                            {
                                // Mark the connection as started
                                _initializeCallback();
                            }
                            else
                            {
                                OnMessage(_connection, sseEvent.Data);
                            }
                            break;
                    }
                }
            }

            private bool TryParseEvent(string line, out SseEvent sseEvent)
            {
                sseEvent = null;

                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    string data = line.Substring("data:".Length).Trim();
                    sseEvent = new SseEvent(EventType.Data, data);
                    return true;
                }
                else if (line.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
                {
                    string data = line.Substring("id:".Length).Trim();
                    sseEvent = new SseEvent(EventType.Id, data);
                    return true;
                }

                return false;
            }

            private class SseEvent
            {
                public SseEvent(EventType type, string data)
                {
                    Type = type;
                    Data = data;
                }

                public EventType Type { get; private set; }
                public string Data { get; private set; }
            }

            private enum EventType
            {
                Id,
                Data
            }

            private class ChunkBuffer
            {
                private int _offset;
                private readonly StringBuilder _buffer;
                private readonly StringBuilder _lineBuilder;

                public ChunkBuffer()
                {
                    _buffer = new StringBuilder();
                    _lineBuilder = new StringBuilder();
                }

                public bool HasChunks
                {
                    get
                    {
                        return _offset < _buffer.Length;
                    }
                }

                public string ReadLine()
                {
                    // TODO: Clean up old processed string
                    for (int i = _offset; i < _buffer.Length; i++, _offset++)
                    {
                        if (_buffer[i] == '\n')
                        {
                            _buffer.Remove(0, _offset + 1);
                            string line = _lineBuilder.ToString();
#if WINDOWS_PHONE
                            _lineBuilder.Length = 0;
#else
                            _lineBuilder.Clear();
#endif
                            _offset = 0;
                            return line;
                        }
                        _lineBuilder.Append(_buffer[i]);
                    }

                    return null;
                }

                public void Add(byte[] buffer, int length)
                {
                    _buffer.Append(Encoding.UTF8.GetString(buffer, 0, length));
                }
            }
        }
    }
}
