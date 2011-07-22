using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace SignalR.SignalBuses {
    internal static class WebRequestAsyncExtensions {
        internal static Task<WebResponse> GetResponseAsync(this WebRequest request) {
            return Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
        }
    }
}