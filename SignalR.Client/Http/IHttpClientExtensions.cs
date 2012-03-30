using System;
using System.Threading.Tasks;

namespace SignalR.Client.Http
{
    public static class IHttpClientExtensions
    {
        public static Task<IResponse> PostAsync(this IHttpClient client, string url, Action<IRequest> prepareRequest)
        {
            return client.PostAsync(url, prepareRequest, postData: null);
        }
    }
}
