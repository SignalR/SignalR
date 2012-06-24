using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Transports.ServerSentEvents
{
    /// <summary>
    /// Event source implementation for .NET. This isn't to the spec but it's enough to support SignalR's
    /// server.
    /// </summary>
    public class EventSourceStreamReader
    {
        private readonly Stream _stream;
        private readonly ChunkBuffer _buffer;
        private readonly object _lockObj = new object();

        private int _reading;
        private Action _setOpened;

        /// <summary>
        /// Invoked when the connection is open.
        /// </summary>
        public Action Opened { get; set; }

        /// <summary>
        /// Invoked when the connection is closed.
        /// </summary>
        public Action Closed { get; set; }

        /// <summary>
        /// Invoked when there's a message if received in the stream.
        /// </summary>
        public Action<SseEvent> Message { get; set; }

        /// <summary>
        /// Invoked when there's an error.
        /// </summary>
        public Action<Exception> Error { get; set; }

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
                return _reading == 1;
            }
        }

        /// <summary>
        /// Starts the reader.
        /// </summary>
        public void Start()
        {
            if (Interlocked.Exchange(ref _reading, 1) == 0)
            {
                _setOpened = () =>
                {
                    Debug.WriteLine("EventSourceReader: Connection Opened");
                    OnOpened();
                };

                // Start the process loop
                Process();
            }
        }

        /// <summary>
        /// Closes the connection and the underlying stream.
        /// </summary>
        public void Close()
        {
            if (Interlocked.Exchange(ref _reading, 0) == 1)
            {
                Debug.WriteLine("EventSourceReader: Connection Closed");
                if (Closed != null)
                {
                    Closed();
                }
            }
        }

        private void Process()
        {
            if (!Processing)
            {
                return;
            }

            var buffer = new byte[4096];
            _stream.ReadAsync(buffer).ContinueWith(task =>
            {
                // When the first get data from the server the trigger the event.
                Interlocked.Exchange(ref _setOpened, () => { }).Invoke();

                if (task.IsFaulted)
                {
                    Exception exception = task.Exception.Unwrap();

                    if (!ExceptionHelper.IsRequestAborted(exception))
                    {
                        if (!(exception is IOException))
                        {
                            OnError(exception);
                        }
                    }

                    Close();
                    return;
                }

                int read = task.Result;

                if (read > 0)
                {
                    // Put chunks in the buffer
                    ProcessBuffer(buffer, read);
                }

                if (read == 0)
                {
                    Close();
                    return;
                }

                // Keep reading the next set of data
                Process();
            });
        }

        private void ProcessBuffer(byte[] buffer, int read)
        {
            lock (_lockObj)
            {
                _buffer.Add(buffer, read);

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

        private void OnError(Exception exception)
        {
            if (Error != null)
            {
                Error(exception);
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
    }
}
