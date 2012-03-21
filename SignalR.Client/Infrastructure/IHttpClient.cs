using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    public interface IHttpClient
    {
        Task<IResponse> GetAsync(string url, Action<IRequest> prepareRequest);
        Task<IResponse> PostAsync(string url, Action<IRequest> prepareRequest, Dictionary<string, string> postData);
    }
}
