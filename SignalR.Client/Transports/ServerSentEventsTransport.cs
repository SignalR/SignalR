﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        private const string ReaderKey = "sse.reader";
        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);

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
            // If we're reconnecting add /connect to the url
            bool reconnect = initializeCallback == null;

            var url = (reconnect ? connection.Url : connection.Url + "connect") + GetReceiveQueryString(connection, data);

            Action<HttpWebRequest> prepareRequest = PrepareRequest(connection);

            HttpHelper.GetAsync(url, request =>
            {
                prepareRequest(request);

                request.Accept = "text/event-stream";

            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    if (errorCallback != null)
                    {
                        errorCallback(task.Exception);
                    }
                    else
                    {
                        // Raise the error event if we failed to reconnect
                        connection.OnError(task.Exception.GetBaseException());
                    }
                }
                else
                {
                    // Get the reseponse stream and read it for messages
                    var stream = task.Result.GetResponseStream();
                    var reader = new AsyncStreamReader(stream,
                                                       connection,
                                                       initializeCallback,
                                                       () =>
                                                       {
                                                           // Wait for a bit before reconnecting
                                                           Thread.Sleep(ReconnectDelay);

                                                           // Now attempt a reconnect
                                                           OpenConnection(connection, data, initializeCallback: null, errorCallback: null);
                                                       });
                    reader.StartReading();

                    // Set the reader for this connection
                    connection.Items[ReaderKey] = reader;
                }
            });
        }

        protected override void OnBeforeAbort(Connection connection)
        {
            // Get the reader from the connection and stop it
            var reader = connection.GetValue<AsyncStreamReader>(ReaderKey);
            if (reader != null)
            {
                // Stop reading data from the stream
                reader.StopReading();

                // Remove the reader
                connection.Items.Remove(ReaderKey);
            }
        }

        private class AsyncStreamReader
        {
            private readonly Stream _stream;
            private readonly ChunkBuffer _buffer;
            private readonly Action _initializeCallback;
            private readonly Action _closeCallback;
            private readonly Connection _connection;
            private int _processingQueue;
            private bool _reading;
            private bool _processingBuffer;

            public AsyncStreamReader(Stream stream, Connection connection, Action initializeCallback, Action closeCallback)
            {
                _initializeCallback = initializeCallback;
                _closeCallback = closeCallback;
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

                        if (!IsRequestAborted(exception))
                        {
                            _connection.OnError(exception);
                        }
                        return;
                    }

                    int read = task.Result;

                    if (read > 0)
                    {
                        // Put chunks in the buffer
                        _buffer.Add(buffer, read);
                    }

                    if (read == 0)
                    {
                        // Stop any reading we're doing
                        StopReading();

                        // Close the stream
                        _stream.Close();

                        // Call the close callback
                        _closeCallback();
                        return;
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
                if (!_reading)
                {
                    return;
                }

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
                    if (!_reading)
                    {
                        return;
                    }

                    ProcessChunks();
                }

                if (_processingQueue > 0)
                {
                    _processingQueue -= total;
                }

                _processingBuffer = false;
            }

            private void ProcessChunks()
            {
                while (_reading && _buffer.HasChunks)
                {
                    string line = _buffer.ReadLine();

                    // No new lines in the buffer so stop processing
                    if (line == null)
                    {
                        break;
                    }

                    if (!_reading)
                    {
                        return;
                    }

                    // Try parsing the sseEvent
                    SseEvent sseEvent;
                    if (!TryParseEvent(line, out sseEvent))
                    {
                        continue;
                    }

                    if (!_reading)
                    {
                        return;
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
                                if (_initializeCallback != null)
                                {
                                    // Mark the connection as started
                                    _initializeCallback();
                                }
                            }
                            else
                            {
                                if (_reading)
                                {
                                    OnMessage(_connection, sseEvent.Data);
                                }
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
