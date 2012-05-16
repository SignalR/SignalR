using System;
#if NET20
	using SignalR.Client.Net20.Infrastructure;
#else
	using System.Threading.Tasks;
#endif


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
