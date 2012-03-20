using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    public class DefaultHttpClient : IHttpClient
    {
        public Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> prepareRequest)
        {
            return HttpHelper.GetAsync(url, prepareRequest);
        }

        public Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> prepareRequest)
        {
            return HttpHelper.PostAsync(url, prepareRequest);
        }

        public Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return HttpHelper.PostAsync(url, prepareRequest, postData);
        }
    }
}
