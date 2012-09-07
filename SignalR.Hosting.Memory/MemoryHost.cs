using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Http;
using SignalR.Hosting.Common;

using IClientRequest = SignalR.Client.Http.IRequest;
using IClientResponse = SignalR.Client.Http.IResponse;
using System.Diagnostics;

namespace SignalR.Hosting.Memory
{
    public class MemoryHost : RoutingHost, IHttpClient, IDisposable
    {
        private readonly CancellationTokenSource _shutDownToken = new CancellationTokenSource();
        private readonly string _mutexPrefix;
        private readonly string _instanceNamePrefix;
        private Mutex _instanceNameMutex;

        public MemoryHost()
            : this(new DefaultDependencyResolver())
        {

        }

        public MemoryHost(IDependencyResolver resolver)
            : base(resolver)
        {
            var process = Process.GetCurrentProcess();
            _mutexPrefix = process.ProcessName;
            _instanceNamePrefix = process.ProcessName + " (PID " + process.Id + "): ";
            InstanceName = GetNextInstanceId();
        }

        public string InstanceName { get; set; }

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
            var uri = new Uri(url);
            PersistentConnection connection;

            if (!_shutDownToken.IsCancellationRequested && TryGetConnection(uri.LocalPath, out connection))
            {
                var tcs = new TaskCompletionSource<IClientResponse>();
                var clientTokenSource = new CancellationTokenSource();
                var request = new Request(uri, clientTokenSource, postData);
                prepareRequest(request);

                Response response = null;
                response = new Response(clientTokenSource.Token, () => tcs.TrySetResult(response));
                var hostContext = new HostContext(request, response);
                var instanceName = GetInstanceName();
                hostContext.Items[HostConstants.InstanceName] = instanceName;
                hostContext.Items[HostConstants.ShutdownToken] = _shutDownToken.Token;

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

        private string GetInstanceName()
        {
            return _instanceNamePrefix + InstanceName;
        }

        private string GetNextInstanceId()
        {
            var instanceId = 0;
            while (true)
            {
                var mutexName = _mutexPrefix + instanceId;
                bool createdMutex;
                // Try to create the mutex with ownership
                try
                {
                    var mutex = new Mutex(true, mutexName, out createdMutex);
                    if (createdMutex)
                    {
                        _instanceNameMutex = mutex;
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Mutex exists but we don't have access to it
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // Name conflict with another native handle
                }
                instanceId++;
            }
            return instanceId.ToString();
        }

        public void Dispose()
        {
            _shutDownToken.Cancel(throwOnFirstException: false);
            _instanceNameMutex.ReleaseMutex();
        }
    }
}
