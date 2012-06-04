using System.Threading.Tasks;
using System.Web;

namespace SignalR.Hosting.AspNet
{
    internal static class HttpResponseExtensions
    {
        public static Task FlushAsync(this HttpResponseBase response)
        {
#if NET45
            if (response.SupportsAsyncFlush)
            {
                return Task.Factory.FromAsync((cb, state) => response.BeginFlush(cb, state), ar => response.EndFlush(ar), null);
            }

            response.Flush();
            return TaskAsyncHelper.Empty;
#else
            response.Flush();
            return TaskAsyncHelper.Empty;
#endif
        }
    }
}
