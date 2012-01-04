using System;
using System.IO;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    internal static class StreamExtensions
    {
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer)
        {
            try
            {
                return Task.Factory.FromAsync((cb, state) => stream.BeginRead(buffer, 0, buffer.Length, cb, state), ar => stream.EndRead(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<int>(ex);
            }
        }
    }
}
