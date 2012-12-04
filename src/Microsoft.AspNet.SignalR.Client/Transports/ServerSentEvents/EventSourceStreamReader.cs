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

        /// <summary>
        /// Invoked when the connection is open.
        /// </summary>
        public Action Opened { get; set; }

        /// <summary>
        /// Invoked when the reader is closed while in the Processing state.
        /// </summary>
        public Action<Exception> Closed { get; set; }

        /// <summary>
        /// Invoked when the reader enters the Stopped state whether or not it was previously in the Processing state.
        /// </summary>
        public Action Disabled { get; set; }

        /// <summary>
        /// Invoked when there's a message if received in the stream.
        /// </summary>
        public Action<SseEvent> Message { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourceStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read event source payloads from.</param>
        public EventSourceStreamReader(Stream stream)
        {
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
                    Debug.WriteLine("EventSourceReader: Connection Opened");
                    OnOpened();
                };

                if (_readBuffer == null)
                {
                    _readBuffer = new byte[4096];
                }

                // Start the process loop
                Process();
            }
        }

        /// <summary>
        /// Closes the connection and the underlying stream.
        /// </summary>
        public void Close()
        {
            Close(exception: null);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The client receives the exception in the Close callback.")]
        private void Process()
        {
        Read:

            if (!Processing)
            {
                return;
            }

            Task<int> readTask = _stream.ReadAsync(_readBuffer);

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
            readTask.Catch(ex => Close(ex))
                    .Then(read =>
                    {
                        if (TryProcessRead(read))
                        {
                            Process();
                        }
                    })
                    .Catch();
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

                    Debug.WriteLine("SSE READ: " + sseEvent);

                    OnMessage(sseEvent);
                }
            }
        }

        private void Close(Exception exception)
        {
            var previousState = Interlocked.Exchange(ref _reading, State.Stopped);

            if (previousState == State.Processing)
            {
                Debug.WriteLine("EventSourceReader: Connection Closed");
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

            if (previousState != State.Stopped && Disabled != null)
            {
                Disabled();
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
