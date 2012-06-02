using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SignalR.Client.Http;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        private const string ReaderKey = "sse.reader";
        private int _initializedCalled;

        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);

        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents")
        {
            ConnectionTimeout = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Time allowed before failing the connect request
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        protected override void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            OpenConnection(connection, data, initializeCallback, errorCallback);
        }

        private void Reconnect(IConnection connection, string data)
        {
            if (connection.IsDisconnecting())
            {
                return;
            }

            // Wait for a bit before reconnecting
            TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
            {
                // Now attempt a reconnect
                OpenConnection(connection, data, initializeCallback: null, errorCallback: null);
            });
        }

        private void OpenConnection(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            // If we're reconnecting add /connect to the url
            bool reconnecting = initializeCallback == null;

            var url = (reconnecting ? connection.Url : connection.Url + "connect") + GetReceiveQueryString(connection, data);

            Action<IRequest> prepareRequest = PrepareRequest(connection);

            Debug.WriteLine("SSE: GET {0}", (object)url);

            _httpClient.GetAsync(url, request =>
            {
                prepareRequest(request);

                request.Accept = "text/event-stream";

            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    var exception = task.Exception.GetBaseException();
                    if (!IsRequestAborted(exception))
                    {
                        if (errorCallback != null &&
                            Interlocked.Exchange(ref _initializedCalled, 1) == 0)
                        {
                            errorCallback(exception);
                        }
                        else if (reconnecting)
                        {
                            // Only raise the error event if we failed to reconnect
                            connection.OnError(exception);
                        }
                    }

                    if (reconnecting)
                    {
                        connection.State = ConnectionState.Reconnecting;

                        // Retry
                        Reconnect(connection, data);
                        return;
                    }
                }
                else
                {
                    // Get the reseponse stream and read it for messages
                    var response = task.Result;
                    var stream = response.GetResponseStream();
                    var reader = new AsyncStreamReader(stream,
                                                       connection,
                                                       () =>
                                                       {
                                                           if (Interlocked.CompareExchange(ref _initializedCalled, 1, 0) == 0)
                                                           {
                                                               initializeCallback();
                                                           }
                                                       },
                                                       () =>
                                                       {
                                                           response.Close();

                                                           connection.State = ConnectionState.Reconnecting;

                                                           Reconnect(connection, data);
                                                       });

                    if (reconnecting)
                    {
                        // Change the status to connected
                        connection.State = ConnectionState.Connected;

                        // Raise the reconnect event if the connection comes back up
                        connection.OnReconnected();
                    }

                    reader.StartReading();

                    // Set the reader for this connection
                    connection.Items[ReaderKey] = reader;
                }
            });

            if (initializeCallback != null)
            {
                TaskAsyncHelper.Delay(ConnectionTimeout).Then(() =>
                {
                    if (Interlocked.CompareExchange(ref _initializedCalled, 1, 0) == 0)
                    {
                        // Stop the connection
                        Stop(connection);

                        // Connection timeout occured
                        errorCallback(new TimeoutException());
                    }
                });
            }
        }

        protected override void OnBeforeAbort(IConnection connection)
        {
            // Get the reader from the connection and stop it
            var reader = connection.GetValue<AsyncStreamReader>(ReaderKey);
            if (reader != null)
            {
                // Stop reading data from the stream, don't close it since we're going to end
                // the request
                reader.StopReading(raiseCloseCallback: false);

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
            private readonly IConnection _connection;
            private int _processingQueue;
            private int _reading;
            private bool _processingBuffer;

            public AsyncStreamReader(Stream stream, IConnection connection, Action initializeCallback, Action closeCallback)
            {
                _initializeCallback = initializeCallback;
                _closeCallback = closeCallback;
                _stream = stream;
                _connection = connection;
                _buffer = new ChunkBuffer();
            }

            public bool Reading
            {
                get
                {
                    return _reading == 1;
                }
            }

            public void StartReading()
            {
                if (Interlocked.Exchange(ref _reading, 1) == 0)
                {
                    ReadLoop();
                }
            }

            public void StopReading(bool raiseCloseCallback = true)
            {
                if (Interlocked.Exchange(ref _reading, 0) == 1)
                {
                    if (raiseCloseCallback)
                    {
                        _closeCallback();
                    }
                }
            }

            private void ReadLoop()
            {
                if (!Reading)
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
                            if (!(exception is IOException))
                            {
                                _connection.OnError(exception);
                            }

                            StopReading();
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
                if (!Reading)
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
                    if (!Reading)
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
                while (Reading && _buffer.HasChunks)
                {
                    string line = _buffer.ReadLine();

                    // No new lines in the buffer so stop processing
                    if (line == null)
                    {
                        break;
                    }

                    if (!Reading)
                    {
                        return;
                    }

                    // Try parsing the sseEvent
                    SseEvent sseEvent;
                    if (!TryParseEvent(line, out sseEvent))
                    {
                        continue;
                    }

                    if (!Reading)
                    {
                        return;
                    }

                    Debug.WriteLine("SSE READ: " + sseEvent);

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
                                if (Reading)
                                {
                                    // We don't care about timedout messages here since it will just reconnect
                                    // as part of being a long running request
                                    bool timedOutReceived;
                                    bool disconnectReceived;

                                    ProcessResponse(_connection, sseEvent.Data, out timedOutReceived, out disconnectReceived);

                                    if (disconnectReceived)
                                    {
                                        _connection.Stop();
                                    }

                                    if (timedOutReceived)
                                    {
                                        return;
                                    }
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

                public override string ToString()
                {
                    return Type + ": " + Data;
                }
            }

            private enum EventType
            {
                Id,
                Data
            }
        }
    }
}
