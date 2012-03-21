using System;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    public static class IHttpClientExtensions
    {
        public static Task<IResponse> PostAsync(this IHttpClient client, string url, Action<IRequest> prepareRequest)
        {
            return client.PostAsync(url, prepareRequest, postData: null);
        }
    }
}
