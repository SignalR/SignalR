using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    public interface IHttpClient
    {
        Task<IHttpResponse> GetAsync(string url, Action<IHttpRequest> prepareRequest);
        Task<IHttpResponse> PostAsync(string url, Action<IHttpRequest> prepareRequest, Dictionary<string, string> postData);
    }
}
