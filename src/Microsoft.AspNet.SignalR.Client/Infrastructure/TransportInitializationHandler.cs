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
    internal class TransportInitializationHandler
    {
        private readonly ThreadSafeInvoker _initializationInvoker;
        private readonly TaskCompletionSource<object> _initializationTask;
        private readonly IConnection _connection;
        private readonly IHttpClient _httpClient;
        private readonly string _connectionData;
        private readonly string _transport;
        private readonly IDisposable _tokenCleanup;
        private readonly TransportHelper _transportHelper;

        private int _state = InitializationState.Initial;

        private static class InitializationState
        {
            public const int Initial = 0;
            public const int AfterConnect = 1;
            public const int Failed = 2;
        }

        public TransportInitializationHandler(IHttpClient httpClient,
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
            _tokenCleanup = disconnectToken.SafeRegister(
                _ => Fail(new OperationCanceledException(Resources.Error_ConnectionCancelled, disconnectToken)),
                state: null);

            TaskAsyncHelper.Delay(connection.TotalTransportConnectTimeout)
                .Then(() =>
                {
                    // don't timeout once connect request has finished
                    if (Interlocked.CompareExchange(ref _state, InitializationState.Failed, InitializationState.Initial) ==
                        InitializationState.Initial)
                    {
                        Fail(new TimeoutException(Resources.Error_TransportTimedOutTryingToConnect));
                    }
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

        public void Fail()
        {
            Fail(new InvalidOperationException(Resources.Error_TransportFailedToConnect));
        }

        public void Fail(Exception ex)
        {
            _initializationInvoker.Invoke(CompleteFail, ex);
        }

        public void InitReceived()
        {
            if (Interlocked.CompareExchange(ref _state, InitializationState.AfterConnect, InitializationState.Initial) ==
                InitializationState.Initial)
            {
                _transportHelper.GetStartResponse(_httpClient, _connection, _connectionData, _transport)
                                .Then(response =>
                                {
                                    var started = _connection.JsonDeserializeObject<JObject>(response)["Response"];
                                    if (started.ToString() == "started")
                                    {
                                        _initializationInvoker.Invoke(CompleteStart);
                                    }
                                    else
                                    {
                                        Fail(new StartException(Resources.Error_StartFailed));
                                    }
                                })
                                .Catch(ex => Fail(new StartException(Resources.Error_StartFailed, ex)), _connection);
            }
        }

        private void CompleteStart()
        {
            Dispatch(() => _initializationTask.TrySetResult(null));
            _tokenCleanup.Dispose();
        }

        private void CompleteFail(Exception ex)
        {
            var previousState = Interlocked.Exchange(ref _state, InitializationState.Failed);

            Dispatch(() =>
            {
                OnFailure();

                // if the transport failed during start request we want to fail with StartException
                // so that AutoTransport does not try other transports
                if (previousState == InitializationState.AfterConnect && !(ex is StartException))
                {
                    ex = new StartException(Resources.Error_StartFailed, ex);
                }
 
                _initializationTask.TrySetUnwrappedException(ex);
            });

            if (_tokenCleanup != null)
            {
                _tokenCleanup.Dispose();
            }
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
