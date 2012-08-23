using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using IClientResponse = SignalR.Client.Http.IResponse;

namespace SignalR.Hosting.Memory
{
    public class Response : IClientResponse, IResponse
    {
        private readonly CancellationToken _clientToken;
        private readonly ResponseStream _stream;

        public Response(CancellationToken clientToken, Action flush)
        {
            _clientToken = clientToken;
            _stream = new ResponseStream(flush);
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

        public Stream OutputStream
        {
            get { return _stream; }
        }

        /// <summary>
        /// Mimics a network stream between client and server.
        /// </summary>
        private class ResponseStream : Stream
        {
            private readonly BlockingCollection<ArraySegment<byte>> _queue;
            private readonly Stack<ArraySegment<byte>> _backlog;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private event Action _onWrite;
            private event Action _onClosed;
            private Action _flush;
            private readonly object _lockObj = new object();

            public ResponseStream(Action flush)
            {
                _flush = flush;
                _queue = new BlockingCollection<ArraySegment<byte>>();
                _backlog = new Stack<ArraySegment<byte>>();
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
                Interlocked.Exchange(ref _flush, () => { }).Invoke();
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
                    ArraySegment<byte> queuedBuffer;

                    // First check to see if there's a backlog
                    if (_backlog.Count > 0)
                    {
                        queuedBuffer = _backlog.Pop();
                    }
                    else
                    {
                        // Read the next chunk from the buffer
                        if (!_queue.TryTake(out queuedBuffer))
                        {
                            queuedBuffer = _queue.Take(CancellationToken);
                        }
                    }

                    int read = Math.Min(count, queuedBuffer.Count);
                    int remainder = queuedBuffer.Count - read;

                    if (remainder > 0)
                    {
                        // Push the remainder back onto the backlog
                        _backlog.Push(new ArraySegment<byte>(queuedBuffer.Array, queuedBuffer.Offset + read, remainder));
                    }

                    Array.Copy(queuedBuffer.Array, queuedBuffer.Offset, buffer, offset, read);

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
                    lock (_lockObj)
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

                int read = Read(buffer, offset, count);

                if (read != 0 || CancellationToken.IsCancellationRequested)
                {
                    lock (_lockObj)
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
                        lock (_lockObj)
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

                return ar;
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                return ((AsyncResult<int>)asyncResult).EndInvoke();
            }

            public override void Close()
            {
                Flush();

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
                _queue.Add(new ArraySegment<byte>(buffer, offset, count));

                if (_onWrite != null)
                {
                    _onWrite();
                }
            }
        }

    }
}
