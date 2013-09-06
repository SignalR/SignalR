// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.WinRT.Http;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class DefaultHttpClient : IHttpClient
    {
        public async Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            var cts = new CancellationTokenSource();
            var handler = new DefaultHttpHandler(prepareRequest, cts.Cancel);
            var client = new HttpClient(handler);
            HttpResponseMessage responseMessage = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return new HttpResponseMessageWrapper(responseMessage);
        }

        public async Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
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
