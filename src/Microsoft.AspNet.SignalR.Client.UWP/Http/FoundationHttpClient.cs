// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Microsoft.AspNet.SignalR.Infrastructure;

using HttpClient = Windows.Web.Http.HttpClient;
using HttpCompletionOption = Windows.Web.Http.HttpCompletionOption;
using HttpMethod = Windows.Web.Http.HttpMethod;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpResponseMessage = System.Net.Http.HttpResponseMessage;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// <see cref="IHttpClient"/> implementation that uses <see cref="HttpClient"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:Implement IDisposable", Justification = "Response task returned to the caller so cannot dispose Http Client")]
    public class FoundationHttpClient : IHttpClient
    {
        private readonly HttpBaseProtocolFilter _httpFilter;

        private HttpClient _longRunningClient;
        private HttpClient _shortRunningClient;
        private IConnection _connection;

        /// <summary>
        /// Creates new <see cref="FoundationHttpClient"/> instance using empty <see cref="HttpBaseProtocolFilter"/>.
        /// </summary>
        public FoundationHttpClient()
            : this(new HttpBaseProtocolFilter())
        {
        }

        /// <summary>
        /// Creates new <see cref="FoundationHttpClient"/> instance using specified <see cref="HttpBaseProtocolFilter"/>.
        /// </summary>
        /// <param name="filter">Filter for <see cref="Windows.Web.Http.HttpClient"/>.</param>
        public FoundationHttpClient(HttpBaseProtocolFilter filter)
        {
            _httpFilter = filter;
        }

        /// <summary>
        /// Initialize the Http Clients
        /// </summary>
        /// <param name="connection">Connection</param>
        public void Initialize(IConnection connection)
        {
            _connection = connection;

            InitializeFilter(_httpFilter, _connection);

            _longRunningClient = CreateHttpClient(_httpFilter);
            _shortRunningClient = CreateHttpClient(_httpFilter);
        }

        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public async Task<IResponse> Get(string url, Action<IRequest> prepareRequest, bool isLongRunning)
        {
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            var requestMessageWrapper = new FoundationHttpRequestMessageWrapper(httpRequestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(requestMessageWrapper);

            var responseMessage = await GetHttpClient(isLongRunning)
                                      .SendRequestAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead)
                                      .AsTask(cts.Token);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpClientException(await ToNetResponse(responseMessage));
            }

            responseDisposer.Set(responseMessage);

            return new FoundationHttpResponseMessageWrapper(responseMessage);
        }

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        public async Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
        {
            var responseDisposer = new Disposer();
            var cts = new CancellationTokenSource();
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));

            httpRequestMessage.Content = postData != null
                                             ? new HttpBufferContent(HttpHelper.ProcessPostData(postData).AsBuffer())
                                             : (IHttpContent) new HttpStringContent(string.Empty);

            var requestMessageWrapper = new FoundationHttpRequestMessageWrapper(httpRequestMessage, () =>
            {
                cts.Cancel();
                responseDisposer.Dispose();
            });

            prepareRequest(requestMessageWrapper);

            var responseMessage = await GetHttpClient(isLongRunning)
                                        .SendRequestAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead)
                                        .AsTask(cts.Token);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new HttpClientException(await ToNetResponse(responseMessage));
            }

            responseDisposer.Set(responseMessage);

            return new FoundationHttpResponseMessageWrapper(responseMessage);
        }

        protected virtual HttpClient CreateHttpClient(HttpBaseProtocolFilter filter)
        {
            return new HttpClient(filter);
        }

        protected virtual void InitializeFilter(HttpBaseProtocolFilter filter, IConnection connection)
        {
            if (connection.Proxy != null)
            {
                throw new NotSupportedException(string.Format(Resources.Error_IConnectionMemberNotSupported, nameof(connection.Proxy)));
            }

            if (connection.Credentials != null)
            {
                throw new NotSupportedException(string.Format(Resources.Error_IConnectionMemberNotSupported, nameof(connection.Credentials)));
            }

            if (connection.CookieContainer != null)
            {
                throw new NotSupportedException(string.Format(Resources.Error_IConnectionMemberNotSupported, nameof(connection.CookieContainer)));
            }
        }

        /// <summary>
        /// Returns the appropriate client based on whether it is a long running request
        /// </summary>
        /// <param name="isLongRunning">Indicates whether the request is long running</param>
        /// <returns></returns>
        private HttpClient GetHttpClient(bool isLongRunning)
        {
            return isLongRunning
                       ? _longRunningClient
                       : _shortRunningClient;
        }

        /// <summary>
        /// Converts <see cref="Windows.Web.Http.HttpResponseMessage"/> to <see cref="HttpResponseMessage"/>
        /// </summary>
        /// <param name="foundationResponse">Message that gets converted</param>
        /// <returns></returns>
        private static async Task<HttpResponseMessage> ToNetResponse(Windows.Web.Http.HttpResponseMessage foundationResponse)
        {
            var netResponse = new HttpResponseMessage((HttpStatusCode) foundationResponse.StatusCode);
            netResponse.Content = new StringContent(await foundationResponse.Content.ReadAsStringAsync());
            netResponse.ReasonPhrase = foundationResponse.ReasonPhrase;
            switch (foundationResponse.Version)
            {
                case HttpVersion.None:
                    netResponse.Version = new Version(0, 0);
                    break;
                case HttpVersion.Http10:
                    netResponse.Version = new Version(1, 0);
                    break;
                case HttpVersion.Http11:
                    netResponse.Version = new Version(1, 1);
                    break;
                case HttpVersion.Http20:
                    netResponse.Version = new Version(2, 0);
                    break;
            }

            foreach (var header in foundationResponse.Headers)
            {
                netResponse.Headers.Add(header.Key, header.Value);
            }

            var foundationRequest = foundationResponse.RequestMessage;
            netResponse.RequestMessage = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod(foundationRequest.Method.Method),
                    foundationRequest.RequestUri);
            netResponse.RequestMessage.Version = new Version(0, 0);
            if (foundationRequest.Content != null)
            {
                netResponse.RequestMessage.Content = new StringContent(await foundationRequest.Content.ReadAsStringAsync());
            }

            foreach (var header in foundationRequest.Headers)
            {
                netResponse.RequestMessage.Headers.Add(header.Key, header.Value);
            }

            foreach (var property in foundationRequest.Properties)
            {
                netResponse.RequestMessage.Properties.Add(property);
            }

            return netResponse;
        }
    }
}