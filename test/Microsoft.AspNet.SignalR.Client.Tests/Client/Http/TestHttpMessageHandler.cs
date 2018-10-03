// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    internal delegate Task<HttpResponseMessage> RequestDelegate(HttpRequestMessage requestMessage, CancellationToken cancellationToken);

    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private List<HttpRequestMessage> _receivedRequests = new List<HttpRequestMessage>();
        private RequestDelegate _app;

        private List<Func<RequestDelegate, RequestDelegate>> _middleware = new List<Func<RequestDelegate, RequestDelegate>>();

        public bool Disposed { get; private set; }

        public IReadOnlyList<HttpRequestMessage> ReceivedRequests
        {
            get
            {
                lock (_receivedRequests)
                {
                    return _receivedRequests.ToArray();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            lock (_receivedRequests)
            {
                _receivedRequests.Add(request);

                if (_app == null)
                {
                    _middleware.Reverse();
                    RequestDelegate handler = BaseHandler;
                    foreach (var middleware in _middleware)
                    {
                        handler = middleware(handler);
                    }

                    _app = handler;
                }
            }

            return await _app(request, cancellationToken);
        }

        public void OnRequest(Func<HttpRequestMessage, Func<Task<HttpResponseMessage>>, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            void OnRequestCore(Func<RequestDelegate, RequestDelegate> middleware)
            {
                _middleware.Add(middleware);
            }

            OnRequestCore(next =>
            {
                return (request, cancellationToken) =>
                {
                    return handler(request, () => next(request, cancellationToken), cancellationToken);
                };
            });
        }

        public void OnGet(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Get, pathAndQuery, handler);
        public void OnPost(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Post, pathAndQuery, handler);
        public void OnPut(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Put, pathAndQuery, handler);
        public void OnDelete(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Delete, pathAndQuery, handler);
        public void OnHead(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Head, pathAndQuery, handler);
        public void OnOptions(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Options, pathAndQuery, handler);
        public void OnTrace(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Trace, pathAndQuery, handler);

        public void OnRequest(HttpMethod method, string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            OnRequest((request, next, cancellationToken) =>
            {
                if (request.Method.Equals(method) && string.Equals(request.RequestUri.PathAndQuery, pathAndQuery))
                {
                    return handler(request, cancellationToken);
                }
                else
                {
                    return next();
                }
            });
        }

        private Task<HttpResponseMessage> BaseHandler(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(new InvalidOperationException($"Http endpoint not implemented: {request.Method} {request.RequestUri}"));
        }
    }
}
