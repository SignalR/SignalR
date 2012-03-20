using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SignalR.Client.Infrastructure
{
    public interface IHttpClient
    {
        Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> action);
        Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> action);
        Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> action, Dictionary<string, string> postData);
    }
}
