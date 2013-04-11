// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    internal class ClientStream : Stream
    {
        private int _position;

        private readonly object _streamLock = new object();
        private readonly MemoryStream _ms = new MemoryStream();
        private readonly SafeCancellationTokenSource _cancelTokenSource = new SafeCancellationTokenSource();
        private readonly Reader _reader = new Reader();

        public ClientStream(INetworkObserver networkObserver)
        {
            networkObserver.OnCancel = _cancelTokenSource.Cancel;
            networkObserver.OnClose = OnClose;
            networkObserver.OnWrite = OnWrite;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            _ms.Close();

            _cancelTokenSource.Dispose();

            base.Close();
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var ar = new AsyncResult<int>(callback, state, _cancelTokenSource.Token);

            _reader.Read(completedSynchronously =>
            {
                // Try to read synchronously
                int read = ReadBuffer(buffer, offset, count);

                // If we're at the end of the stream or we've read some data then return it
                ar.SetAsCompleted(read, completedSynchronously);
            });

            return ar;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((AsyncResult<int>)asyncResult).EndInvoke();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        private int ReadBuffer(byte[] buffer, int offset, int count)
        {
            lock (_streamLock)
            {
                byte[] underlyingBuffer = _ms.GetBuffer();

                int canRead = (int)_ms.Length - _position;

                if (canRead == 0)
                {
                    // REVIEW: Consider trimming the buffer after consuming up to _position
                    return 0;
                }

                int read = Math.Min(count, canRead);

                Array.Copy(underlyingBuffer, _position, buffer, offset, read);

                // Mark these bytes as consumed
                _reader.Consume(read);

                _position += read;

                return read;
            }
        }

        private void OnWrite(ArraySegment<byte> buffer)
        {
            lock (_streamLock)
            {
                _ms.Write(buffer.Array, buffer.Offset, buffer.Count);
            }

            _reader.Add(buffer.Count);
        }

        private void OnClose()
        {
            // Pretend like there's an extra byte to be read from the buffer to force a read
            _reader.Add(1);
        }

        private class Reader
        {
            private int _bytesRemaining;
            private readonly Queue<Action<bool>> _pendingReadCallbacks = new Queue<Action<bool>>();
            private readonly object _lockObj = new object();

            public void Add(int count)
            {
                lock (_lockObj)
                {
                    _bytesRemaining += count;

                    if (_pendingReadCallbacks.Count > 0)
                    {
                        // Trigger any pending callbacks
                        Action<bool> callback = _pendingReadCallbacks.Dequeue();

                        TryExecuteRead(callback, completedSynchronously: false);
                    }
                }
            }

            public void Read(Action<bool> callback)
            {
                lock (_lockObj)
                {
                    if (!TryExecuteRead(callback, completedSynchronously: true))
                    {
                        // Enqueue the pending callback
                        _pendingReadCallbacks.Enqueue(callback);
                    }
                }
            }

            public void Consume(int read)
            {
                lock (_lockObj)
                {
                    // Decrement the number of bytes consumed
                    _bytesRemaining -= read;
                }
            }

            private bool TryExecuteRead(Action<bool> callback, bool completedSynchronously)
            {
                if (_bytesRemaining > 0)
                {
                    // Trigger the read callback
                    callback(completedSynchronously);

                    return true;
                }

                return false;
            }
        }
    }
}
