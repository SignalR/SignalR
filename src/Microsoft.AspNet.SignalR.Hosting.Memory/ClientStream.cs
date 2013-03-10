using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    public class ClientStream : Stream
    {
        private int _readPos;

        private readonly MemoryStream _ms = new MemoryStream();
        private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
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
            // TODO: Change this to throw new NotImplementedException(), when we remove all sync reads
            return ReadBuffer(buffer, offset, count);
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

                return read;
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
            lock (_ms)
            {
                byte[] underlyingBuffer = _ms.GetBuffer();

                int canRead = (int)_ms.Length - _readPos;

                if (canRead == 0)
                {
                    // REVIEW: Consider trimming the buffer after consuming up to _readPos
                    return 0;
                }

                int read = Math.Min(count, canRead);

                Array.Copy(underlyingBuffer, _readPos, buffer, offset, read);

                _readPos += read;

                return read;
            }
        }

        private void OnWrite(ArraySegment<byte> buffer)
        {
            lock (_ms)
            {
                _ms.Write(buffer.Array, buffer.Offset, buffer.Count);

                _reader.NotifyRead(buffer.Count);
            }
        }

        private void OnClose()
        {
            // Pretend like there's an extra byte to be read from the buffer to force a read
            _reader.NotifyRead(1);
        }

        private class Reader
        {
            private int _bytesRemaining;
            private readonly Queue<Func<bool, int>> _pendingReadCallbacks = new Queue<Func<bool, int>>();

            public void NotifyRead(int count)
            {
                Interlocked.Add(ref _bytesRemaining, count);

                lock (_pendingReadCallbacks)
                {
                    if (_pendingReadCallbacks.Count > 0)
                    {
                        // Trigger any pending callbacks
                        Func<bool, int> callback = _pendingReadCallbacks.Dequeue();

                        TryExecuteRead(callback, completedSynchronously: false);
                    }
                }
            }

            public void Read(Func<bool, int> callback)
            {
                if (!TryExecuteRead(callback, completedSynchronously: true))
                {
                    lock (_pendingReadCallbacks)
                    {
                        // Enqueue the pending callback
                        _pendingReadCallbacks.Enqueue(callback);
                    }
                }
            }

            private bool TryExecuteRead(Func<bool, int> callback, bool completedSynchronously)
            {
                if (_bytesRemaining > 0)
                {
                    // Trigger the read callback
                    int consumed = callback(completedSynchronously);

                    // Decrement the number of bytes consumed
                    Interlocked.Add(ref _bytesRemaining, -consumed);

                    return true;
                }

                return false;
            }
        }
    }
}
