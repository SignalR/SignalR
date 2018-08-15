// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET40

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal static class StreamExtensions
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset, int length)
        {
            return FromAsync(cb => stream.BeginRead(buffer, offset, length, cb, null), ar => stream.EndRead(ar));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared class.")]
        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset, int length)
        {
            return FromAsync(cb => stream.BeginWrite(buffer, offset, length, cb, null), WrapEndWrite(stream));
        }

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
            var tcs = new DispatchingTaskCompletionSource<T>();
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
        private static void CompleteAsync<T>(DispatchingTaskCompletionSource<T> tcs, IAsyncResult ar, Func<IAsyncResult, T> end)
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
    }
}
#elif NET45 || NETSTANDARD1_3 || NETSTANDARD2_0
// Not needed on this framework
#else
#error Unsupported framework.
#endif
