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
        private HttpClient _longRunningClient;
        private HttpClient _shortRunningClient;

        private IConnection _connection;

        /// <summary>
        /// Initialize the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        public void Initialize(IConnection connection)
        {
            _connection = connection;

            var handler = new DefaultHttpHandler(connection);

            _longRunningClient = new HttpClient(handler);
            _shortRunningClient = new HttpClient(handler);
        }

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            HttpClient httpClient = isLongRunning ? _longRunningClient : _shortRunningClient;

            return httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                 .Then(responseMessage =>
                 {
                     if (responseMessage.IsSuccessStatusCode)
                     {
                         responseDisposer.Set(responseMessage);
                     }
                     else
                     {
                         throw new HttpClientException(responseMessage);
                     }

                     return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                 });
        }

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
        {
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            if (postData == null)
            {
                requestMessage.Content = new StringContent(String.Empty);
            }
            else
            {
                requestMessage.Content = new FormUrlEncodedContent(postData);
            }

            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            HttpClient httpClient = isLongRunning ? _longRunningClient : _shortRunningClient;

            return httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token).
                Then(responseMessage =>
                {
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        responseDisposer.Set(responseMessage);
                    }
                    else
                    {
                        throw new HttpClientException(responseMessage);
                    }

                    return (IResponse)new HttpResponseMessageWrapper(responseMessage);
                });
        }
    }
}
