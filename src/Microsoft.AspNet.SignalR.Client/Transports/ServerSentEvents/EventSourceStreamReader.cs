// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents
{
    /// <summary>
    /// Event source implementation for .NET. This isn't to the spec but it's enough to support SignalR's
    /// server.
    /// </summary>
    public class EventSourceStreamReader : AsyncStreamReader
    {
        private readonly ChunkBuffer _buffer;
        private readonly IConnection _connection;

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
            : base(stream)
        {
            _connection = connection;
            _buffer = new ChunkBuffer();

            Data = ProcessBuffer;
        }

        private void ProcessBuffer(ArraySegment<byte> readBuffer)
        {
            lock (BufferLock)
            {
                _buffer.Add(readBuffer);

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

                    _connection.Trace(TraceLevels.Messages, "SSE: OnMessage({0})", sseEvent);

                    OnMessage(sseEvent);
                }
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
