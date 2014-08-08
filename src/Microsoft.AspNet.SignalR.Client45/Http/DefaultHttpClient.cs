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
    [SuppressMessage("Microsoft.Design", "CA1001:Implement IDisposable", Justification = "Response task returned to the caller so cannot dispose Http Client")]
    public class DefaultHttpClient : IHttpClient
    {
        private HttpClient _longRunningClient;
        private HttpClient _shortRunningClient;

        private IConnection _connection;

        /// <summary>
        /// Initialize the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler cannot be disposed before response is disposed")]
        public void Initialize(IConnection connection)
        {
            _connection = connection;

            _longRunningClient = new HttpClient(CreateHandler());

            // Disabling the Http Client timeout 
            _longRunningClient.Timeout = TimeSpan.FromMilliseconds(-1.0);

            _shortRunningClient = new HttpClient(CreateHandler());
            _shortRunningClient.Timeout = TimeSpan.FromMilliseconds(-1.0);
        }

        protected virtual HttpMessageHandler CreateHandler()
        {
            return new DefaultHttpHandler(_connection);
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
            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            HttpClient httpClient = GetHttpClient(isLongRunning);

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
            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            if (postData == null)
            {
                requestMessage.Content = new StringContent(String.Empty);
            }
            else
            {
                requestMessage.Content = new ByteArrayContent(HttpHelper.ProcessPostData(postData));
            }

            var request = new HttpRequestMessageWrapper(requestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(request);

            HttpClient httpClient = GetHttpClient(isLongRunning);

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
        /// Returns the appropriate client based on whether it is a long running request
        /// </summary>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns></returns>
        private HttpClient GetHttpClient(bool isLongRunning)
        {
            return isLongRunning ? _longRunningClient : _shortRunningClient;
        }
    }
}
