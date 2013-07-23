// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
using Microsoft.Owin;
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
        private IAppBuilder _appBuilder;
        private AppFunc _appFunc;
        private string _instanceName;
        private readonly Lazy<string> _defaultInstanceName;

        public void Initialize(SignalR.Client.IConnection connection)
        {
        }

        public MemoryHost()
        {
            _shutDownToken = _shutDownTokenSource.Token;
            _defaultInstanceName = new Lazy<string>(() => Process.GetCurrentProcess().GetUniqueInstanceName(_shutDownToken));
        }

        public void Configure(Action<IAppBuilder> startup)
        {
            if (startup == null)
            {
                throw new ArgumentNullException("startup");
            }

            _appBuilder = new AppBuilder();

            _appBuilder.Properties[OwinConstants.ServerCapabilities] = new Dictionary<string, object>();
            _appBuilder.Properties[OwinConstants.HostOnAppDisposing] = _shutDownToken;
            _appBuilder.Properties[OwinConstants.HostAppNameKey] = InstanceName;

            startup(_appBuilder);

            _appFunc = Build(_appBuilder);
        }

        public string InstanceName
        {
            get
            {
                return _instanceName ?? _defaultInstanceName.Value;
            }
            set
            {
                _instanceName = value;
            }
        }

        public Task<IClientResponse> Get(string url)
        {
            return Get(url, disableWrites: false);
        }

        public Task<IClientResponse> Get(string url, bool disableWrites)
        {
            return ProcessRequest("GET", url, req => { }, null, disableWrites);
        }

        public Task<IClientResponse> Post(string url, IDictionary<string, string> postData, bool isLongRunning)
        {
            return ((IHttpClient)this).Post(url, req => { }, postData, isLongRunning);
        }

        Task<IClientResponse> IHttpClient.Get(string url, Action<IClientRequest> prepareRequest, bool isLongRunning)
        {
            return ProcessRequest("GET", url, prepareRequest, postData: null);
        }

        Task<IClientResponse> IHttpClient.Post(string url, Action<IClientRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
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

            // REVIEW: Should we add a new method to the IClientResponse to trip this?
            var clientTokenSource = new SafeCancellationTokenSource();

            var env = new OwinEnvironment(_appBuilder.Properties);

            // Request specific setup
            var uri = new Uri(url);

            env[OwinConstants.RequestProtocol] = "HTTP/1.1";
            env[OwinConstants.CallCancelled] = clientTokenSource.Token;
            env[OwinConstants.RequestMethod] = httpMethod;
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = uri.LocalPath;
            env[OwinConstants.RequestQueryString] = uri.Query.Length > 0 ? uri.Query.Substring(1) : String.Empty;
            env[OwinConstants.RequestScheme] = uri.Scheme;
            env[OwinConstants.RequestBody] = GetRequestBody(postData);
            var headers = new Dictionary<string, string[]>();
            env[OwinConstants.RequestHeaders] = headers;

            headers.SetHeader("X-Server", "MemoryHost");
            headers.SetHeader("X-Server-Name", InstanceName);

            if (httpMethod == "POST")
            {
                headers.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            var networkObservable = new NetworkObservable(disableWrites);
            var clientStream = new ClientStream(networkObservable, clientTokenSource);
            var serverStream = new ServerStream(networkObservable);

            var response = new Response(clientStream);

            // Trigger the tcs on flush. This mimicks the client side
            networkObservable.OnFlush = () => tcs.TrySetResult(response);

            // Run the client function to initialize the request
            prepareRequest(new Request(env, networkObservable.Cancel));

            env[OwinConstants.ResponseBody] = serverStream;
            env[OwinConstants.ResponseHeaders] = new Dictionary<string, string[]>();

            _appFunc(env).ContinueWith(task =>
            {
                var owinResponse = new OwinResponse(env);
                if (!IsSuccessStatusCode(owinResponse.StatusCode))
                {
                    tcs.TrySetException(new InvalidOperationException("Unsuccessful status code " + owinResponse.StatusCode));
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

                // Close the server stream when the request has ended
                serverStream.Close();
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
                    _shutDownTokenSource.Cancel();

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

        private static bool IsSuccessStatusCode(int statusCode)
        {
            // If it's unset just return true
            if (statusCode == 0)
            {
                return true;
            }

            return (statusCode >= 200) && (statusCode <= 299);
        }
    }
}
