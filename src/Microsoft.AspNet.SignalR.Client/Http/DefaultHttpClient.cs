// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

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
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public Task<IResponse> Get(string url, Action<IRequest> prepareRequest)
        {
            var disposerResponse = new Disposer();
            var cts = new CancellationTokenSource();

            var handler = new DefaultHttpHandler(prepareRequest, () =>
            {
                cts.Cancel();
                disposerResponse.Dispose();
            });

            var client = new HttpClient(handler);

            return client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, cts.Token)
                 .Then(responseMessage =>
                 {
                     try
                     {
                         responseMessage.EnsureSuccessStatusCode();
                         disposerResponse.Set(responseMessage);
                     }
                     catch
                     {
                         throw new HttpClientException(responseMessage);
                     }

                     return (IResponse)new HttpResponseMessageWrapper(responseMessage, client);
                 });
        }

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData)
        {
            var disposerResponse = new Disposer();
            var cts = new CancellationTokenSource();

            var handler = new DefaultHttpHandler(prepareRequest, () =>
            {
                cts.Cancel();
                disposerResponse.Dispose();
            });

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            if (postData == null)
            {
                request.Content = new StringContent(String.Empty);
            }
            else
            {
                request.Content = new FormUrlEncodedContent(postData);
            }

            return client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).
                Then(responseMessage =>
                {
                    try
                    {
                        responseMessage.EnsureSuccessStatusCode();
                        disposerResponse.Set(responseMessage);
                    }
                    catch
                    {
                        throw new HttpClientException(responseMessage);
                    }

                    return (IResponse)new HttpResponseMessageWrapper(responseMessage, client);
                });
        }
    }
}
