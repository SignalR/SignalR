﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
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
                return !_stream.CancellationToken.IsCancellationRequested &&
                       !_clientToken.IsCancellationRequested;
            }
        }

        public string ContentType
        {
            get;
            set;
        }

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
            _stream.Write(data.Array, data.Offset, data.Count);
        }

        /// <summary>
        /// Mimics a network stream between client and server.
        /// </summary>
        private class ResponseStream : Stream
        {
            private MemoryStream _currentStream;
            private int _readPos;
            private readonly CancellationTokenSource _cancellationTokenSource;

            private event Action _onWrite;
            private event Action _onClosed;
            private readonly object _completedLock = new object();
            private readonly object _writeLock = new object();

            public ResponseStream()
            {
                _currentStream = new MemoryStream();
                _cancellationTokenSource = new CancellationTokenSource();
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
                    return _cancellationTokenSource.Token;
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

                Action closedHandler = null;
                closedHandler = () =>
                {
                    lock (_completedLock)
                    {
                        if (!ar.IsCompleted)
                        {
                            // Set the response to 0 if we've closed
                            ar.SetAsCompleted(0, false);
                        }

                        _onClosed -= closedHandler;
                    }
                };

                _onClosed += closedHandler;

                if (CancellationToken.IsCancellationRequested)
                {
                    ar.SetAsCompleted(0, true);
                    return ar;
                }

                lock (_writeLock)
                {
                    int read = Read(buffer, offset, count);

                    if (read != 0 || CancellationToken.IsCancellationRequested)
                    {
                        lock (_completedLock)
                        {
                            if (!ar.IsCompleted)
                            {
                                ar.SetAsCompleted(read, true);
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
                _cancellationTokenSource.Cancel(throwOnFirstException: false);

                if (_onClosed != null)
                {
                    _onClosed();
                }

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
