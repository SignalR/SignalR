using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Http;
using SignalR.Hosting.Common;

using IClientRequest = SignalR.Client.Http.IRequest;
using IClientResponse = SignalR.Client.Http.IResponse;

namespace SignalR.Hosting.Memory
{
    public class MemoryHost : RoutingHost, IHttpClient
    {
        public MemoryHost()
            : base()
        {

        }

        public MemoryHost(IDependencyResolver resolver)
            : base(resolver)
        {

        }

        Task<IClientResponse> IHttpClient.GetAsync(string url, Action<IClientRequest> prepareRequest)
        {
            return ProcessRequest(url, prepareRequest, postData: null);
        }

        Task<IClientResponse> IHttpClient.PostAsync(string url, Action<IClientRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return ProcessRequest(url, prepareRequest, postData);
        }

        private Task<IClientResponse> ProcessRequest(string url, Action<IClientRequest> prepareRequest, Dictionary<string, string> postData)
        {
            var uri = new Uri(url);

            PersistentConnection connection;
            if (TryGetConnection(uri.LocalPath, out connection))
            {
                var tcs = new TaskCompletionSource<IClientResponse>();
                var clientTokenSource = new CancellationTokenSource();
                var request = new Request(uri, clientTokenSource, postData);
                prepareRequest(request);

                Response response = null;
                response = new Response(clientTokenSource.Token, () => tcs.TrySetResult(response));
                var hostContext = new HostContext(request, response);

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

                    response.Close();
                });

                return tcs.Task;
            }

            return TaskAsyncHelper.FromError<IClientResponse>(new InvalidOperationException("Not a valid end point"));
        }
    }
}
