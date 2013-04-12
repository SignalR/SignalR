﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;
using Owin;
using Owin.Builder;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class MemoryHost : IHttpClient, IDisposable
    {
        private readonly CancellationTokenSource _shutDownTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _shutDownToken;
        private int _disposed;
        private AppFunc _appFunc;

        public MemoryHost()
        {
            _shutDownToken = _shutDownTokenSource.Token;
        }

        public void Configure(Action<IAppBuilder> startup)
        {
            if (startup == null)
            {
                throw new ArgumentNullException("startup");
            }

            var builder = new AppBuilder();

            builder.Properties[OwinConstants.ServerCapabilities] = new Dictionary<string, object>();
            builder.Properties[OwinConstants.HostOnAppDisposing] = _shutDownToken;
            builder.Properties[OwinConstants.HostAppNameKey] = InstanceName ?? Process.GetCurrentProcess().GetUniqueInstanceName(_shutDownToken);

            startup(builder);

            _appFunc = Build(builder);
        }

        public string InstanceName { get; set; }

        public Task<IClientResponse> Get(string url)
        {
            return Get(url, disableWrites: false);
        }

        public Task<IClientResponse> Get(string url, bool disableWrites)
        {
            return ProcessRequest("GET", url, req => { }, null, disableWrites);
        }

        public Task<IClientResponse> Post(string url, IDictionary<string, string> postData)
        {
            return ((IHttpClient)this).Post(url, req => { }, postData);
        }

        Task<IClientResponse> IHttpClient.Get(string url, Action<IClientRequest> prepareRequest)
        {
            return ProcessRequest("GET", url, prepareRequest, postData: null);
        }

        Task<IClientResponse> IHttpClient.Post(string url, Action<IClientRequest> prepareRequest, IDictionary<string, string> postData)
        {
            return ProcessRequest("POST", url, prepareRequest, postData);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The cancellation token is disposed when the request ends")]
        private Task<IClientResponse> ProcessRequest(string httpMethod, string url, Action<IClientRequest> prepareRequest, IDictionary<string, string> postData, bool disableWrites = false)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            if (_appFunc == null)
            {
                throw new InvalidOperationException();
            }

            if (_shutDownToken.IsCancellationRequested)
            {
                return TaskAsyncHelper.FromError<IClientResponse>(new InvalidOperationException("Service unavailable"));
            }

            var tcs = new TaskCompletionSource<IClientResponse>();
            var clientTokenSource = new SafeCancellationTokenSource();

            var env = new Dictionary<string, object>();

            // Server specific setup
            env[OwinConstants.Version] = "1.0";

            // Request specific setup
            var uri = new Uri(url);

            env[OwinConstants.CallCancelled] = clientTokenSource.Token;
            env[OwinConstants.RequestMethod] = httpMethod;
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = uri.LocalPath;
            env[OwinConstants.RequestQueryString] = uri.Query.Length > 0 ? uri.Query.Substring(1) : String.Empty;
            env[OwinConstants.RequestScheme] = uri.Scheme;
            env[OwinConstants.RequestBody] = GetRequestBody(postData);
            var headers = new Dictionary<string, string[]>();
            env[OwinConstants.RequestHeaders] = headers;

            if (httpMethod == "POST")
            {
                headers.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            // Run the client function to initialize the request
            prepareRequest(new Request(env, clientTokenSource.Cancel));

            Response response = null;
            response = new Response(disableWrites, () => tcs.TrySetResult(response), clientTokenSource.Token);
            env[OwinConstants.ResponseBody] = response.GetResponseStream();
            env[OwinConstants.ResponseHeaders] = new Dictionary<string, string[]>();

            _appFunc(env).ContinueWith(task =>
            {
                object statusCode;
                if (env.TryGetValue(OwinConstants.ResponseStatusCode, out statusCode) &&
                    (int)statusCode == 403)
                {
                    tcs.TrySetException(new InvalidOperationException("Forbidden"));
                }
                else if (task.IsFaulted)
                {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(response);
                }

                response.Close();
                clientTokenSource.Dispose();
            });

            return tcs.Task;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _shutDownTokenSource.Cancel(throwOnFirstException: false);

                    _shutDownTokenSource.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller owns the stream")]
        private static Stream GetRequestBody(IDictionary<string, string> postData)
        {
            var ms = new MemoryStream();
            if (postData != null)
            {
                bool first = true;
                var writer = new StreamWriter(ms);
                writer.AutoFlush = true;
                foreach (var item in postData)
                {
                    if (!first)
                    {
                        writer.Write("&");
                    }
                    writer.Write(item.Key);
                    writer.Write("=");
                    writer.Write(UrlEncoder.UrlEncode(item.Value));
                    writer.WriteLine();
                    first = false;
                }

                ms.Seek(0, SeekOrigin.Begin);
            }
            return ms;
        }

        private static AppFunc Build(IAppBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return (AppFunc)builder.Build(typeof(AppFunc));
        }
    }
}
