// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class TransportInitializationHandler
    {
        private readonly ThreadSafeInvoker _initializationInvoker;
        private readonly TaskCompletionSource<object> _initializationTask;
        private readonly IConnection _connection;
        private readonly IHttpClient _httpClient;
        private readonly string _connectionData;
        private readonly string _transport;
        private readonly IDisposable _tokenCleanup;
        private readonly TransportHelper _transportHelper;

        internal TransportInitializationHandler(IHttpClient httpClient,
                                              IConnection connection,
                                              string connectionData,
                                              string transport,
                                              CancellationToken disconnectToken, 
                                              TransportHelper transportHelper)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            _connection = connection;
            _httpClient = httpClient;
            _connectionData = connectionData;
            _transport = transport;
            _transportHelper = transportHelper;

            _initializationTask = new TaskCompletionSource<object>();
            _initializationInvoker = new ThreadSafeInvoker();

            // Default event
            OnFailure = () => { };

            // We want to fail if the disconnect token is tripped while we're waiting on initialization
            _tokenCleanup = disconnectToken.SafeRegister(_ =>
            {
                Fail();
            },
            state: null);

            TaskAsyncHelper.Delay(connection.TotalTransportConnectTimeout).Then(() =>
            {
                Fail(new TimeoutException(Resources.Error_TransportTimedOutTryingToConnect));
            });
        }

        public event Action OnFailure;

        public Task Task
        {
            get
            {
                return _initializationTask.Task;
            }
        }

        public void InitReceived()
        {
            _initializationInvoker.Invoke(Start);
        }

        public void Fail()
        {
            Fail(new InvalidOperationException(Resources.Error_TransportFailedToConnect));
        }

        public void Fail(Exception ex)
        {
            _initializationInvoker.Invoke(CompleteFail, ex);
        }

        private void Start()
        {
            _transportHelper.GetStartResponse(_httpClient, _connection, _connectionData, _transport).Then(response =>
            {
                var started = _connection.JsonDeserializeObject<JObject>(response)["Response"];
                if (started.ToString() == "started")
                {
                    CompleteStart();
                }
                else
                {
                    CompleteFail(new StartException(Resources.Error_StartFailed));
                }
            }).Catch(ex =>
            {
                CompleteFail(new StartException(Resources.Error_StartFailed, ex));
            });
        }

        private void CompleteStart()
        {
            Dispatch(() => _initializationTask.SetResult(null));
            _tokenCleanup.Dispose();
        }

        private void CompleteFail(Exception ex)
        {
            Dispatch(() =>
            {
                OnFailure();
                _initializationTask.SetException(ex);
            });

            _tokenCleanup.Dispose();
        }

        private static void Dispatch(Action callback)
        {
#if NETFX_CORE
            Task.Run(() =>
#else
            ThreadPool.QueueUserWorkItem(_ =>
#endif
            callback());
        }
    }
}
