using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Client.Http
{
    public class DefaultHttpClient : IHttpClient
    {
        public Task<IResponse> GetAsync(string url, Action<IRequest> prepareRequest)
        {
            return HttpHelper.GetAsync(url, request => prepareRequest(new HttpWebRequestWrapper(request)))
                             .Then(response => (IResponse)new HttpWebResponseWrapper(response));
        }

        public Task<IResponse> PostAsync(string url, Action<IRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return HttpHelper.PostAsync(url, request => prepareRequest(new HttpWebRequestWrapper(request)), postData)
                             .Then(response => (IResponse)new HttpWebResponseWrapper(response));
        }
    }
}
