namespace Gate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Threading.Tasks;
    using System.Threading;
    using Gate.Utils;
    using System.Diagnostics;

    internal class ResponseStream : Stream
    {
        private object internalLock = new object();
        private Queue<byte[]> bufferQueue = new Queue<byte[]>();
        private Stream outputStream;
        private CancellationToken cancel;

        private int state;

        private const int buffering = 0; // Just copy write data to the buffer.
        private const int offloading = 1; // Emptying buffer to output stream, new writes still go into the buffer.
        private const int streaming = 2; // Buffer is empty and output stream is available, bypass the buffer and go directly to the output stream.
        private const int closed = 3; // Disposed, throw.

        public ResponseStream(CancellationToken cancel)
        {
            this.cancel = cancel;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
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

        public override int ReadTimeout
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

        public override int ReadByte()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override int EndRead(IAsyncResult asyncResult)
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

        private void ValidateData(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (offset > data.Length || offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", offset, string.Empty);
            }
            if (count > data.Length - offset || count < 0)
            {
                throw new ArgumentOutOfRangeException("count", count, string.Empty);
            }
        }

        private bool BufferData(byte[] data, int offset, int count)
        {
            ValidateData(data, offset, count);
            lock (internalLock)
            {
                if (state == buffering || state == offloading)
                {
                    byte[] bufferCopy = new byte[count];
                    Buffer.BlockCopy(data, offset, bufferCopy, 0, count);
                    bufferQueue.Enqueue(bufferCopy);
                    return true;
                }
                else if (state == streaming)
                {
                    return false;
                }
                else
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!BufferData(buffer, offset, count))
            {
                outputStream.Write(buffer, offset, count);
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (!BufferData(buffer, offset, count))
            {
                return outputStream.BeginWrite(buffer, offset, count, callback, state);
            }

            // Buffered sync
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(state);
            tcs.TrySetResult(null);
            if (callback != null)
            {
                callback(tcs.Task);
            }
            return tcs.Task;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            if (state == buffering || state == offloading)
            {
                ((Task)asyncResult).Wait();
            }
            
            if (state == streaming)
            {
                outputStream.EndWrite(asyncResult);
            }

            if (state == closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public override void Flush()
        {
            if (state == streaming)
            {
                outputStream.Flush();
            }
        }

        internal Task TransitionFromBufferedToUnbuffered(Stream output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            int priorState = Interlocked.CompareExchange(ref state, offloading, buffering);
            if (priorState == closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (priorState != buffering)
            {
                throw new InvalidOperationException();
            }

            outputStream = output;
            return DrainBufferAsync();
        }

        private Task DrainBufferAsync()
        {
            if (state == closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            byte[] nextBuffer;

            lock (internalLock)
            {
                if (bufferQueue.Count == 0)
                {
                    Interlocked.CompareExchange(ref state, streaming, offloading);
                    return TaskHelpers.Completed();
                }

                nextBuffer = bufferQueue.Dequeue();
            }

            return outputStream.WriteAsync(nextBuffer, 0, nextBuffer.Length, cancel)
                .Then(() => DrainBufferAsync(), cancel, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                state = closed;
                // We don't need to close the output stream, the server will take care of that.
            }
            base.Dispose(disposing);
        }
    }
}
