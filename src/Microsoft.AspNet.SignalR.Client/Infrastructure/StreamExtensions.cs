// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal static class StreamExtensions
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer)
        {
#if NETFX_CORE || NET45
            return stream.ReadAsync(buffer, 0, buffer.Length);
#else
            return FromAsync(cb => stream.BeginRead(buffer, 0, buffer.Length, cb, null), ar => stream.EndRead(ar));
#endif
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared class.")]
        public static Task WriteAsync(this Stream stream, byte[] buffer)
        {
#if NETFX_CORE || NET45
            return stream.WriteAsync(buffer, 0, buffer.Length);
#else
            return FromAsync(cb => stream.BeginWrite(buffer, 0, buffer.Length, cb, null), WrapEndWrite(stream));
#endif
        }

#if !(NETFX_CORE || NET45)

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared class.")]
        private static Func<IAsyncResult, object> WrapEndWrite(Stream stream)
        {
            return ar =>
            {
                stream.EndWrite(ar);
                return null;
            };
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        private static Task<T> FromAsync<T>(Func<AsyncCallback, IAsyncResult> begin, Func<IAsyncResult, T> end)
        {
            var tcs = new TaskCompletionSource<T>();
            try
            {
                var result = begin(ar =>
                {
                    if (!ar.CompletedSynchronously)
                    {
                        CompleteAsync(tcs, ar, end);
                    }
                });

                if (result.CompletedSynchronously)
                {
                    CompleteAsync(tcs, result, end);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        private static void CompleteAsync<T>(TaskCompletionSource<T> tcs, IAsyncResult ar, Func<IAsyncResult, T> end)
        {
            try
            {
                tcs.TrySetResult(end(ar));
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }
#endif
    }
}
