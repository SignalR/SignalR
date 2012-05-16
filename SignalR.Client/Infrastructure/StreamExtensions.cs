using System;
using System.IO;
#if NET20
using SignalR.Client.Net20.Infrastructure;
#else
	using System.Threading.Tasks;
#endif

namespace SignalR.Client.Infrastructure
{
    internal static class StreamExtensions
    {
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer)
        {
#if NETFX_CORE
            return stream.ReadAsync(buffer, 0, buffer.Length);
#else
            try
            {
                return Task.Factory.FromAsync((cb, state) => stream.BeginRead(buffer, 0, buffer.Length, cb, state), ar => stream.EndRead(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<int>(ex);
            }
#endif
        }

        public static Task WriteAsync(this Stream stream, byte[] buffer)
        {
#if NETFX_CORE
            return stream.WriteAsync(buffer, 0, buffer.Length);
#else
            try
            {
                return Task.Factory.FromAsync((cb, state) => stream.BeginWrite(buffer, 0, buffer.Length, cb, state), ar => stream.EndWrite(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
#endif
        }
    }
}
