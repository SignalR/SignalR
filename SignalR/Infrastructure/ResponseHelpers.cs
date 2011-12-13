using System.Text;
using System.Threading.Tasks;
using System.Web;
using System;

namespace SignalR.Infrastructure
{
    public static class ResponseHelpers
    {
        public static Task WriteAsync(this HttpResponseBase response, string s)
        {
            // The OutputStream on HttpResponse/HttpReponseBase does not implement a true async write,
            // so we're just going to do it sync and return an Empty task here.
            response.Write(s);
            return TaskAsyncHelper.Empty;
        }
    }
}