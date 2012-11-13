// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Calls to close with dispose the stream")]
    public class Response : IClientResponse, IResponse
    {
        private readonly CancellationToken _clientToken;
        private readonly ResponseStream _stream;
        private Action _flush;

        public Response(CancellationToken clientToken, Action flush)
        {
            _clientToken = clientToken;
            _stream = new ResponseStream();
            _flush = flush;
        }

        public string ReadAsString()
        {
            return new StreamReader(_stream).ReadToEnd();
        }

        public Stream GetResponseStream()
        {
            return _stream;
        }

        public void Close()
        {
            _stream.Close();
        }

        public bool IsClientConnected
        {
            get
            {
                return !_clientToken.IsCancellationRequested;
            }
        }

        public string ContentType { get; set; }

        public bool DisableWrites { get; set; }

        public Task FlushAsync()
        {
            Interlocked.Exchange(ref _flush, () => { }).Invoke();
            return TaskAsyncHelper.Empty;
        }

        public Task EndAsync()
        {
            return FlushAsync();
        }

        public void Write(ArraySegment<byte> data)
        {
            if (!DisableWrites)
            {
                _stream.Write(data.Array, data.Offset, data.Count);
            }
        }

        /// <summary>
        /// Mimics a network stream between client and server.
        /// </summary>
        private class ResponseStream : Stream
        {
            private MemoryStream _currentStream;
            private int _readPos;
            private readonly SafeCancellationTokenSource _cancellationTokenSource;
            private readonly CancellationToken _cancellationToken;

            private event Action _onWrite;
            private readonly object _completedLock = new object();
            private readonly object _writeLock = new object();

            public ResponseStream()
            {
                _currentStream = new MemoryStream();
                _cancellationTokenSource = new SafeCancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;
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
                    return true;
                }
            }

            public CancellationToken CancellationToken
            {
                get
                {
                    return _cancellationToken;
                }
            }

            public override void Flush()
            {

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
                try
                {
                    byte[] underlyingBuffer = _currentStream.GetBuffer();

                    int canRead = (int)_currentStream.Length - _readPos;

                    if (canRead == 0)
                    {
                        // Consider trimming the buffer after consuming up to _readPos
                        return 0;
                    }

                    int read = Math.Min(count, canRead);

                    Array.Copy(underlyingBuffer, _readPos, buffer, offset, read);

                    _readPos += read;

                    return read;
                }
                catch (OperationCanceledException)
                {
                    return 0;
                }
                catch (ObjectDisposedException)
                {
                    return 0;
                }
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                var ar = new AsyncResult<int>(callback, state);

                IDisposable registration = CancellationToken.SafeRegister(asyncResult =>
                {
                    lock (_completedLock)
                    {
                        if (!asyncResult.IsCompleted)
                        {
                            asyncResult.SetAsCompleted(0, false);
                        }
                    }
                },
                ar);

                // If a write occurs after a synchronous read attempt and before the writeHandler is attached,
                // the writeHandler could miss a write.
                lock (_writeLock)
                {
                    int read = Read(buffer, offset, count);

                    if (read != 0)
                    {
                        lock (_completedLock)
                        {
                            if (!ar.IsCompleted)
                            {
                                ar.SetAsCompleted(read, true);

                                registration.Dispose();
                            }
                        }
                    }
                    else
                    {
                        Action writeHandler = null;
                        writeHandler = () =>
                        {
                            lock (_completedLock)
                            {
                                if (!ar.IsCompleted)
                                {
                                    read = Read(buffer, offset, count);
                                    ar.SetAsCompleted(read, false);

                                    registration.Dispose();
                                }

                                _onWrite -= writeHandler;
                            }
                        };

                        _onWrite += writeHandler;
                    }

                }
                return ar;
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                return ((AsyncResult<int>)asyncResult).EndInvoke();
            }

            public override void Close()
            {
                _cancellationTokenSource.Cancel();

                _cancellationTokenSource.Dispose();

                base.Close();
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
                lock (_writeLock)
                {
                    _currentStream.Write(buffer, offset, count);

                    if (_onWrite != null)
                    {
                        _onWrite();
                    }
                }
            }
        }
    }
}
