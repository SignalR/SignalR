// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal static class StreamExtensions
    {
        public static IConnection _connection;

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer)
        {
#if NETFX_CORE || NET45
            return stream.ReadAsync(buffer, 0, buffer.Length);
#else
            //return FromAsync(cb => stream.BeginRead(buffer, 0, buffer.Length, cb, null), ar => stream.EndRead(ar));
            if (_connection != null)
            {
                _connection.Trace(TraceLevels.Events, stream.GetType().FullName);
            }            
            
            AsyncCallback cb = ar =>
            {
                if (_connection != null)
                {
                    _connection.Trace(TraceLevels.Events, "cb");
                }
            };

            if (_connection != null)
            {
                _connection.Trace(TraceLevels.Events, "stream.BeginRead 1");
            }
            
            IAsyncResult iar = stream.BeginRead(buffer, 0, buffer.Length, cb, null);
            
            if (_connection != null)
            {
                _connection.Trace(TraceLevels.Events, "stream.BeginRead 2");
            }

            var tcs = new TaskCompletionSource<int>();

            try
            {
                if (_connection != null)
                {
                    _connection.Trace(TraceLevels.Events, "stream.EndRead 1");
                }
                var r = stream.EndRead(iar);
                if (_connection != null)
                {
                    _connection.Trace(TraceLevels.Events, "stream.EndRead 2");
                }
                tcs.TrySetResult(r);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
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
                    if (_connection != null)
                    {
                        _connection.Trace(TraceLevels.Events, "begin");
                    }
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
            if (_connection != null)
            {
                _connection.Trace(TraceLevels.Events, "CompleteAsync");
            }

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
