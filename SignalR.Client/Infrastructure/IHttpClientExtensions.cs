using System;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    public static class IHttpClientExtensions
    {
        public static Task<IHttpResponse> PostAsync(this IHttpClient client, string url, Action<IHttpRequest> prepareRequest)
        {
            return client.PostAsync(url, prepareRequest, postData: null);
        }
    }
}
