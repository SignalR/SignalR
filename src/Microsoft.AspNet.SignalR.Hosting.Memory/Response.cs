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
    internal class Response : IClientResponse
    {
        private readonly ResponseStream _stream;

        public Response(bool disableWrites, Action flush)
        {
            _stream = new ResponseStream(disableWrites, flush);
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

            private readonly Action _flush;
            private readonly bool _disableWrites;

            public ResponseStream(bool disableWrites, Action flush)
            {
                _disableWrites = disableWrites;
                _flush = flush;
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
                _flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                Flush();
                return TaskAsyncHelper.Empty;
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

                IDisposable registration = CancellationToken.SafeRegister(cancelState =>
                {
                    var asyncResult = (AsyncResult<int>)cancelState;
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
                if (_disableWrites)
                {
                    return;
                }

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
