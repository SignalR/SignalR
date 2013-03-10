using System;
using System.IO;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    public class ServerStream : Stream
    {
        private readonly NetworkObservable _networkObservable;
        private readonly Action _flush;

        public ServerStream(NetworkObservable networkObservable, Action flush)
        {
            _networkObservable = networkObservable;
            _flush = flush;
        }

        public override bool CanRead
        {
            get
            {
                return false;
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            _networkObservable.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            _flush();
        }

        public override void Close()
        {
            _networkObservable.Close();

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

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
