using System.Net;
using System.Threading.Tasks;

namespace SignalR.ScaleOut {
    internal static class WebRequestAsyncExtensions {
        internal static Task<WebResponse> GetResponseAsync(this WebRequest request) {
            return Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
        }
    }
}