using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Gate.Utils
{
    // #if NET40
    internal static class Net40Extensions
    {
        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset, int count, 
            CancellationToken cancel = default(CancellationToken))
        {
            cancel.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<object>();
            var sr = stream.BeginWrite(buffer, offset, count, ar =>
            {
                if (ar.CompletedSynchronously)
                {
                    return;
                }
                try
                {
                    stream.EndWrite(ar);
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    // Assume errors were caused by cancelation.
                    if (cancel.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                    }

                    tcs.SetException(ex);
                }
            }, null);

            if (sr.CompletedSynchronously)
            {
                try
                {
                    stream.EndWrite(sr);
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    // Assume errors were caused by cancelation.
                    if (cancel.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                    }

                    tcs.SetException(ex);
                }
            }
            return tcs.Task;
        }

        public static Task FlushAsync(this Stream stream)
        {
            stream.Flush();
            return TaskHelpers.Completed();
        }

        // Copy the source stream to the destination.  The source is always disposed at the end, regardless of success or failure.
        // It is the consumers responsiblity to dispose of the destination.
        public static Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancel = default(CancellationToken))
        {
            return CopyToAsync(source, destination, null, cancel);
        }

        public static Task CopyToAsync(this Stream source, Stream destination, int? bytesRemaining, CancellationToken cancel = default(CancellationToken))
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            int bufferSize = 64 * 1024;
            if (bytesRemaining.HasValue)
            {
                bufferSize = Math.Min(bytesRemaining.Value, bufferSize);
            }

            CopyOperation copy = new CopyOperation()
            {
                source = source,
                destination = destination,
                bytesRemaining = bytesRemaining,
                buffer = new byte[bufferSize],
                cancel = cancel,
                complete = new TaskCompletionSource<object>()
            };

            return copy.Start();
        }

        private class CopyOperation
        {
            internal Stream source;
            internal Stream destination;
            internal byte[] buffer;
            internal int? bytesRemaining;
            internal CancellationToken cancel;
            internal TaskCompletionSource<object> complete;

            internal Task Start()
            {
                ReadNextSegment();
                return complete.Task;
            }

            private void Complete()
            {
                complete.TrySetResult(null);
                source.Dispose();
            }

            private void Fail(Exception ex)
            {
                complete.TrySetException(ex);
                source.Dispose();
            }

            private void ReadNextSegment()
            {
                // The natural end of the range.
                if (bytesRemaining.HasValue && bytesRemaining.Value <= 0)
                {
                    Complete();
                    return;
                }

                try
                {
                    cancel.ThrowIfCancellationRequested();
                    int readLength = buffer.Length;
                    if (bytesRemaining.HasValue)
                    {
                        readLength = Math.Min(bytesRemaining.Value, readLength);
                    }
                    IAsyncResult async = source.BeginRead(buffer, 0, readLength, ReadCallback, null);

                    if (async.CompletedSynchronously)
                    {
                        int read = source.EndRead(async);
                        WriteToOutputStream(read);
                    }
                }
                catch (Exception ex)
                {
                    Fail(ex);
                }
            }

            private void ReadCallback(IAsyncResult async)
            {
                if (async.CompletedSynchronously)
                {
                    return;
                }

                try
                {
                    cancel.ThrowIfCancellationRequested();
                    int read = source.EndRead(async);
                    WriteToOutputStream(read);
                }
                catch (Exception ex)
                {
                    Fail(ex);
                }
            }

            private void WriteToOutputStream(int count)
            {
                if (bytesRemaining.HasValue)
                {
                    bytesRemaining -= count;
                }

                // End of the source stream.
                if (count == 0)
                {
                    Complete();
                    return;
                }

                try
                {
                    cancel.ThrowIfCancellationRequested();
                    IAsyncResult async = destination.BeginWrite(buffer, 0, count, WriteCallback, null);
                    if (async.CompletedSynchronously)
                    {
                        destination.EndWrite(async);
                        ReadNextSegment();
                    }
                }
                catch (Exception ex)
                {
                    Fail(ex);
                }
            }

            private void WriteCallback(IAsyncResult async)
            {
                if (async.CompletedSynchronously)
                {
                    return;
                }

                try
                {
                    cancel.ThrowIfCancellationRequested();
                    destination.EndWrite(async);
                    ReadNextSegment();
                }
                catch (Exception ex)
                {
                    Fail(ex);
                }
            }
        }
    }
    // #endif
}
