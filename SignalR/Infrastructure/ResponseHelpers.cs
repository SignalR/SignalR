using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Infrastructure
{
    public static class ResponseHelpers
    {
        public static Task WriteAsync(this HttpResponseBase response, string s)
        {
            var bytes = UTF8Encoding.UTF8.GetBytes(s);
            return Task.Factory.FromAsync(
                (cb, state) => {
                    if (response.IsClientConnected)
                    {
                        return response.OutputStream.BeginWrite(bytes, 0, bytes.Length, cb, state);
                    }
                    cb(TaskAsyncHelper.Empty);
                    return TaskAsyncHelper.Empty;
                },
                response.OutputStream.EndWrite,
                null);
        }
    }
}