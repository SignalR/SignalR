using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Infrastructure;

namespace SignalR.Hosting.Memory
{
    public class Response : IHttpResponse, IResponse
    {
        private string _nonStreamingData;
        private readonly CancellationToken _clientToken;
        private bool _ended;
        private readonly Action _startSending;
        private int _streaming;
        private FollowStream _responseStream = new FollowStream();

        public Response(CancellationToken clientToken, Action startSending)
        {
            _clientToken = clientToken;
            _startSending = startSending;
        }

        public string ReadAsString()
        {
            return _nonStreamingData;
        }

        public Stream GetResponseStream()
        {
            return _responseStream;
        }

        public void Close()
        {
            _responseStream.Close();
        }

        public bool IsClientConnected
        {
            get
            {
                return !_responseStream.Ended && !_clientToken.IsCancellationRequested;
            }
        }

        public string ContentType
        {
            get;
            set;
        }

        public Task WriteAsync(string data)
        {
            if (!_ended)
            {
                _responseStream.Write(data);
            }

            if (Interlocked.Exchange(ref _streaming, 1) == 0)
            {
                _startSending();
            }

            return TaskAsyncHelper.Empty;
        }

        public Task EndAsync(string data)
        {
            _nonStreamingData = data;
            return TaskAsyncHelper.Empty;
        }

        private class FollowStream : Stream
        {
            private readonly MemoryStream _ms;
            private int _readPosition;
            private event Action _onWrite;

            public FollowStream()
            {
                _ms = new MemoryStream();
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

            public bool Ended { get; private set; }

            public override void Flush()
            {
                throw new NotImplementedException();
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
                byte[] followingBuffer = _ms.GetBuffer();
                int max = Math.Min(count, (int)_ms.Length);
                Array.Copy(followingBuffer, _readPosition, buffer, offset, max);
                int read = Math.Abs(_readPosition - max);
                _readPosition += read;
                return read;
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                var ar = new AsyncResult<int>(callback, state);
                
                int read = Read(buffer, offset, count);

                if (read != 0 || Ended)
                {
                    ar.SetAsCompleted(read, true);
                }
                else
                {
                    Action handler = null;
                    handler = () =>
                    {
                        read = Read(buffer, offset, count);
                        ar.SetAsCompleted(read, false);
                        _onWrite -= handler;
                    };
                    _onWrite += handler;
                }
                return ar;
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                return ((AsyncResult<int>)asyncResult).EndInvoke();
            }

            public override void Close()
            {
                Ended = true;
                _ms.Close();
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

            public void Write(string data)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                Write(bytes, 0, bytes.Length);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _ms.Write(buffer, offset, count);

                if (_onWrite != null)
                {
                    _onWrite();
                }
            }
        }
    }
}
