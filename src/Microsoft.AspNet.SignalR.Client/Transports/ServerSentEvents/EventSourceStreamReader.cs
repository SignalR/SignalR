// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents
{
    /// <summary>
    /// Event source implementation for .NET. This isn't to the spec but it's enough to support SignalR's
    /// server.
    /// </summary>
    public class EventSourceStreamReader
    {
        private readonly Stream _stream;
        private readonly ChunkBuffer _buffer;
        private readonly object _bufferLock = new object();
        private byte[] _readBuffer;


        private int _reading;
        private Action _setOpened;
        private readonly IConnection _connection;

        /// <summary>
        /// Invoked when the connection is open.
        /// </summary>
        public Action Opened { get; set; }

        /// <summary>
        /// Invoked when the reader is closed while in the Processing state.
        /// </summary>
        public Action<Exception> Closed { get; set; }

        /// <summary>
        /// Invoked when there's a message if received in the stream.
        /// </summary>
        public Action<SseEvent> Message { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourceStreamReader"/> class.
        /// </summary>
        /// <param name="connection">The connection associated with this event source</param>
        /// <param name="stream">The stream to read event source payloads from.</param>
        public EventSourceStreamReader(IConnection connection, Stream stream)
        {
            _connection = connection;
            _stream = stream;
            _buffer = new ChunkBuffer();
        }

        private bool Processing
        {
            get
            {
                return _reading == State.Processing;
            }
        }

        /// <summary>
        /// Starts the reader.
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _reading, State.Processing, State.Initial) == State.Initial)
            {
                _setOpened = () =>
                {
                    _connection.Trace.WriteLine("EventSourceReader: Connection Opened");
                    OnOpened();
                };

                // FIX: Potential memory leak if Close is called between the CompareExchange and here.
                _readBuffer = new byte[4096];

                // Start the process loop
                Process();
            }
        }

        /// <summary>
        /// Closes the connection and the underlying stream.
        /// </summary>
        private void Close()
        {
            Close(exception: null);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The client receives the exception in the Close callback.")]
        private void Process()
        {
        Read:
            Task<int> readTask;
            lock (_bufferLock)
            {
                if (Processing && _readBuffer != null)
                {
                    readTask = _stream.ReadAsync(_readBuffer);
                }
                else
                {
                    return;
                }
            }

            if (readTask.IsCompleted)
            {
                try
                {
                    // Observe all exceptions
                    readTask.Wait();

                    int read = readTask.Result;

                    if (TryProcessRead(read))
                    {
                        goto Read;
                    }
                }
                catch (Exception ex)
                {
                    Close(ex);
                }
            }
            else
            {
                ReadAsync(readTask);
            }
        }

        private void ReadAsync(Task<int> readTask)
        {
            readTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Close(task.Exception);
                }
                else if (task.IsCanceled)
                {
                    Close(new OperationCanceledException());
                }
                else
                {
                    if (TryProcessRead(task.Result))
                    {
                        Process();
                    }
                }
            }, 
            TaskContinuationOptions.ExecuteSynchronously);
        }

        private bool TryProcessRead(int read)
        {
            Interlocked.Exchange(ref _setOpened, () => { }).Invoke();

            if (read > 0)
            {
                // Put chunks in the buffer
                ProcessBuffer(read);

                return true;
            }
            else if (read == 0)
            {
                Close();
            }

            return false;
        }

        private void ProcessBuffer(int read)
        {
            lock (_bufferLock)
            {
                if (_readBuffer != null)
                {
                    _buffer.Add(_readBuffer, read);
                }

                while (_buffer.HasChunks)
                {
                    string line = _buffer.ReadLine();

                    // No new lines in the buffer so stop processing
                    if (line == null)
                    {
                        break;
                    }

                    SseEvent sseEvent;
                    if (!SseEvent.TryParse(line, out sseEvent))
                    {
                        continue;
                    }

                    _connection.Trace.WriteLine("SSE READ: " + sseEvent);

                    OnMessage(sseEvent);
                }
            }
        }

        private void Close(Exception exception)
        {
            var previousState = Interlocked.Exchange(ref _reading, State.Stopped);

            if (previousState != State.Stopped)
            {
                _connection.Trace.WriteLine("EventSourceReader: Connection Closed");
                if (Closed != null)
                {
                    if (exception != null)
                    {
                        exception = exception.Unwrap();
                    }

                    Closed(exception);
                }

                lock (_bufferLock)
                {
                    // Release the buffer
                    _readBuffer = null;
                }
            }
        }

        private void OnOpened()
        {
            if (Opened != null)
            {
                Opened();
            }
        }

        private void OnMessage(SseEvent sseEvent)
        {
            if (Message != null)
            {
                Message(sseEvent);
            }
        }

        private static class State
        {
            public const int Initial = 0;
            public const int Processing = 1;
            public const int Stopped = 2;
        }
    }
}
