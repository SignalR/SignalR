// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// The default <see cref="IHttpClient"/> implementation.
    /// </summary>
    public class DefaultHttpClient : IHttpClient
    {
        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether it is a long running request</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            IRequest req = null;
            return HttpHelper.GetAsync(url, request =>
            {
                req = new HttpWebRequestWrapper(request);
                prepareRequest(req);
            }
            ).Then(response => (IResponse)new HttpWebResponseWrapper(response));
        }

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether it is a long running request</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
        {
            IRequest req = null;
            return HttpHelper.PostAsync(url, request =>
            {
                req = new HttpWebRequestWrapper(request);
                prepareRequest(req);
            },
            postData).Then(response => (IResponse)new HttpWebResponseWrapper(response));
        }
    }
}
