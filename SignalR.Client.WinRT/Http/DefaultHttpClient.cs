using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.WinRT.Http;

namespace SignalR.Client.Http
{
    public class DefaultHttpClient : IHttpClient
    {
        public async Task<IResponse> GetAsync(string url, Action<IRequest> prepareRequest)
        {
            var cts = new CancellationTokenSource();
            var handler = new DefaultHttpHandler(prepareRequest, cts.Cancel);
            var client = new HttpClient(handler);
            HttpResponseMessage responseMessage = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return new HttpResponseMessageWrapper(responseMessage);
        }

        public async Task<IResponse> PostAsync(string url, Action<IRequest> prepareRequest, Dictionary<string, string> postData)
        {
            var cts = new CancellationTokenSource();
            var handler = new DefaultHttpHandler(prepareRequest, cts.Cancel);
            var client = new HttpClient(handler);
            var req = new HttpRequestMessage(HttpMethod.Post, url);

            if (postData == null)
            {
                req.Content = new StringContent(String.Empty);
            }
            else
            {
                req.Content = new FormUrlEncodedContent(postData);
            }

            HttpResponseMessage responseMessage = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return new HttpResponseMessageWrapper(responseMessage);
        }
    }
}
