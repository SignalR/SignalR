// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Hosting.Common;
using Microsoft.AspNet.SignalR.Infrastructure;
using IClientRequest = Microsoft.AspNet.SignalR.Client.Http.IRequest;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    public class MemoryHost : RoutingHost, IHttpClient, IDisposable
    {
        private readonly CancellationTokenSource _shutDownTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _shutDownToken;
        private int _disposed;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The resolver is disposed when the shutdown token triggers")]
        public MemoryHost()
            : this(new DefaultDependencyResolver())
        {

        }

        public MemoryHost(IDependencyResolver resolver)
            : base(resolver)
        {
            _shutDownToken = _shutDownTokenSource.Token;

            resolver.InitializeHost(Process.GetCurrentProcess().GetUniqueInstanceName(_shutDownToken), _shutDownToken);

            User = Thread.CurrentPrincipal;
        }

        public string InstanceName { get; set; }

        public IPrincipal User { get; set; }

        Task<IClientResponse> IHttpClient.GetAsync(string url, Action<IClientRequest> prepareRequest)
        {
            return ProcessRequest(url, prepareRequest, postData: null);
        }

        Task<IClientResponse> IHttpClient.PostAsync(string url, Action<IClientRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return ProcessRequest(url, prepareRequest, postData);
        }

        public Task<IClientResponse> ProcessRequest(string url, Action<IClientRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return ProcessRequest(url, prepareRequest, postData, disableWrites: false);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The cancellation token is disposed when the request ends")]
        public Task<IClientResponse> ProcessRequest(string url, Action<IClientRequest> prepareRequest, Dictionary<string, string> postData, bool disableWrites)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            var uri = new Uri(url);
            PersistentConnection connection;

            if (!_shutDownToken.IsCancellationRequested && TryGetConnection(uri.LocalPath, out connection))
            {
                var tcs = new TaskCompletionSource<IClientResponse>();
                var clientTokenSource = new SafeCancellationTokenSource();
                var request = new Request(uri, clientTokenSource.Cancel, postData, User);
                prepareRequest(request);

                Response response = null;
                response = new Response(clientTokenSource.Token, () => tcs.TrySetResult(response))
                {
                    DisableWrites = disableWrites
                };
                var hostContext = new HostContext(request, response);

                hostContext.Items[HostConstants.ShutdownToken] = _shutDownTokenSource.Token;

                connection.Initialize(DependencyResolver, hostContext);

                connection.ProcessRequestAsync(hostContext).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception);
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

            return TaskAsyncHelper.FromError<IClientResponse>(new InvalidOperationException("Not a valid end point"));
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
    }
}
