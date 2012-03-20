using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    public class DefaultHttpClient : IHttpClient
    {
        public Task<IHttpResponse> GetAsync(string url, Action<IHttpRequest> prepareRequest)
        {
            return HttpHelper.GetAsync(url, request => prepareRequest(new HttpWebRequestWrapper(request)))
                             .Then(response => (IHttpResponse)new HttpWebResponseWrapper(response));
        }

        public Task<IHttpResponse> PostAsync(string url, Action<IHttpRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return HttpHelper.PostAsync(url, request => prepareRequest(new HttpWebRequestWrapper(request)), postData)
                             .Then(response => (IHttpResponse)new HttpWebResponseWrapper(response));
        }
    }
}
