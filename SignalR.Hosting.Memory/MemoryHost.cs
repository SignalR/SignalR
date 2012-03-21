using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Infrastructure;
using SignalR.Hosting.Common;
using SignalR.Infrastructure;

namespace SignalR.Hosting.Memory
{
    public class MemoryHost : DefaultHost, IHttpClient
    {
        public MemoryHost()
            : this(new DefaultDependencyResolver())
        {

        }

        public MemoryHost(IDependencyResolver resolver)
            : base(resolver)
        {

        }

        Task<IHttpResponse> IHttpClient.GetAsync(string url, Action<IHttpRequest> prepareRequest)
        {
            return ProcessRequest(url, prepareRequest, postData: null);
        }

        Task<IHttpResponse> IHttpClient.PostAsync(string url, Action<IHttpRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return ProcessRequest(url, prepareRequest, postData);
        }

        private Task<IHttpResponse> ProcessRequest(string url, Action<IHttpRequest> prepareRequest, Dictionary<string, string> postData)
        {
            var uri = new Uri(url);

            PersistentConnection connection;
            if (TryGetConnection(uri.LocalPath, out connection))
            {
                var tcs = new TaskCompletionSource<IHttpResponse>();
                var clientTokenSource = new CancellationTokenSource();
                var request = new Request(uri, clientTokenSource, postData);
                prepareRequest(request);

                Response response = null;
                response = new Response(clientTokenSource.Token, () => tcs.TrySetResult(response));
                var hostContext = new HostContext(request, response, null);

                connection.Initialize(DependencyResolver);

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
                });

                return tcs.Task;
            }

            return TaskAsyncHelper.FromError<IHttpResponse>(new InvalidOperationException("Not a valid end point"));
        }
    }
}
