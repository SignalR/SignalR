using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Hosting.Self.Infrastructure
{
    public static class ResponseExtensions
    {
        public static Task NotFound(this HttpListenerResponse response)
        {
            response.StatusCode = 404;
            return TaskAsyncHelper.Empty;
        }

        public static Task ServerError(this HttpListenerResponse response, Exception exception)
        {
            response.StatusCode = 500;
            return response.WriteAsync(exception.ToString());
        }

        public static Task WriteAsync(this HttpListenerResponse response, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return WriteAsync(response, new ArraySegment<byte>(bytes));
        }

        public static Task WriteAsync(this HttpListenerResponse response, ArraySegment<byte> buffer)
        {
#if NET45
            return response.OutputStream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count);
#else
            try
            {
                return Task.Factory.FromAsync((cb, state) => response.OutputStream.BeginWrite(buffer.Array, buffer.Offset, buffer.Count, cb, state),
                                               ar => response.OutputStream.EndWrite(ar),
                                               null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
#endif
        }

        public static void CloseSafe(this HttpListenerResponse response)
        {
            try
            {
                response.Close();
            }
            catch
            {
                // Swallow exceptions while closing just in case the connection goes away
            }
        }
    }
}
