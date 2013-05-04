// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BookSleeve;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Redis
{
    /// <summary>
    /// Uses Redis pub-sub to scale-out SignalR applications in web farms.
    /// </summary>
    public class RedisMessageBus : ScaleoutMessageBus
    {
        private const int DefaultBufferSize = 1000;

        private readonly int _db;
        private readonly string _key;
        private readonly Func<RedisConnection> _connectionFactory;
        private readonly TraceSource _trace;

        private RedisConnection _connection;
        private RedisSubscriberConnection _channel;
        private int _state;
        private readonly object _callbackLock = new object();

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Reviewed")]
        public RedisMessageBus(IDependencyResolver resolver, RedisScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _connectionFactory = configuration.ConnectionFactory;
            _db = configuration.Database;
            _key = configuration.EventKey;

            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(RedisMessageBus).Name];

            ReconnectDelay = TimeSpan.FromSeconds(2);
            ConnectWithRetry();
        }

        public TimeSpan ReconnectDelay { get; set; }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            var tcs = new TaskCompletionSource<object>();

            SendImpl(messages, tcs);

            return tcs.Task;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var oldState = Interlocked.Exchange(ref _state, State.Disposing);

                switch (oldState)
                {
                    case State.Connected:
                        Shutdown();
                        break;
                    case State.Closed:
                    case State.Disposing:
                        // No-op
                        break;
                    case State.Disposed:
                        Interlocked.Exchange(ref _state, State.Disposed);
                        break;
                    default:
                        break;
                }
            }

            base.Dispose(disposing);
        }

        private void Shutdown()
        {
            _trace.TraceInformation("Shutdown()");

            if (_channel != null)
            {
                _channel.Unsubscribe(_key);
                _channel.Close(abort: true);
            }

            if (_connection != null)
            {
                _connection.Close(abort: true);
            }

            Interlocked.Exchange(ref _state, State.Disposed);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is set in the tcs")]
        private void SendImpl(IList<Message> messages, TaskCompletionSource<object> tcs)
        {
        go:
            Task<long?> task = _connection.Strings.GetInt64(_db, _key);

            RedisTransaction transaction = null;

            // Dispose the transaction after everything runs
            tcs.Task.Finally(state =>
            {
                if (transaction != null)
                {
                    _trace.TraceVerbose("Transaction disposed");

                    transaction.Dispose();
                }
            },
            null);

            if (task.IsCompleted)
            {
                try
                {
                    task.Wait();

                    _trace.TraceVerbose("CreateTransaction({0})", task.Result);

                    transaction = _connection.CreateTransaction();

                    Task<bool> transactionTask = ExecuteTransaction(transaction, task.Result, messages);

                    if (transactionTask.IsCompleted)
                    {
                        transactionTask.Wait();

                        bool success = transactionTask.Result;

                        if (success)
                        {
                            OnTransactionComplete(transaction, success, messages, tcs);
                        }
                        else
                        {
                            _trace.TraceVerbose("Transaction failed. Retrying...");

                            _trace.TraceVerbose("Transaction disposed");

                            transaction.Dispose();

                            goto go;
                        }
                    }
                    else
                    {
                        OnTransactionCompleting(transaction, transactionTask, messages, tcs);
                    }
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }
            else
            {
                task.Then(oldId =>
                {
                    _trace.TraceVerbose("CreateTransaction({0})", oldId);

                    transaction = _connection.CreateTransaction();

                    Task<bool> transactionTask = ExecuteTransaction(transaction, oldId, messages);

                    OnTransactionCompleting(transaction, transactionTask, messages, tcs);
                })
                .ContinueWithNotComplete(tcs);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is set in the tcs")]
        private void OnTransactionCompleting(RedisTransaction transaction, Task<bool> transactionTask, IList<Message> messages, TaskCompletionSource<object> tcs)
        {
            if (transactionTask.IsCompleted)
            {
                try
                {
                    OnTransactionComplete(transaction, transactionTask.Result, messages, tcs);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }
            else
            {
                transactionTask.Then(result => OnTransactionComplete(transaction, result, messages, tcs))
                               .ContinueWithNotComplete(tcs);
            }
        }

        private void OnTransactionComplete(RedisTransaction transaction, bool success, IList<Message> messages, TaskCompletionSource<object> tcs)
        {
            if (success)
            {
                _trace.TraceVerbose("Transaction completed successfully");

                tcs.TrySetResult(null);
            }
            else
            {
                _trace.TraceVerbose("Transaction failed. Retrying...");

                _trace.TraceVerbose("Transaction disposed");

                // Dispose the transaction
                transaction.Dispose();

                SendImpl(messages, tcs);
            }
        }

        private Task<bool> ExecuteTransaction(RedisTransaction transaction, long? oldId, IList<Message> messages)
        {
            _trace.TraceVerbose("ExecuteTransaction({0})", oldId);

            // New target id
            long newId = (oldId ?? 0) + 1;

            // TODO: Don't do this everytime
            byte[] data = RedisMessage.ToBytes(newId, messages);

            // These don't need to be observed
            transaction.AddCondition(Condition.KeyEquals(_db, _key, oldId));
            transaction.Strings.Increment(_db, _key);
            transaction.Publish(_key, data);

            // Execute the transaction
            return transaction.Execute();
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            _trace.TraceInformation("OnConnectionClosed()");

            AttemptReconnect(exception: new RedisConnectionClosedException());
        }

        private void OnConnectionError(object sender, ErrorEventArgs e)
        {
            _trace.TraceError("OnConnectionError - " + e.Cause + ". " + e.Exception.GetBaseException());

            AttemptReconnect(e.Exception);
        }

        private void AttemptReconnect(Exception exception)
        {
            // Change the state to closed and retry connecting
            var oldState = Interlocked.CompareExchange(ref _state,
                                                       State.Closed,
                                                       State.Connected);
            if (oldState == State.Connected)
            {
                _trace.TraceInformation("Attempting reconnect...");

                // Let the base class know that an error occurred
                OnError(0, exception);

                // Retry until the connection reconnects
                ConnectWithRetry();
            }
        }

        private void OnMessage(string key, byte[] data)
        {
            // locked to avoid overlapping calls (even though we have set the mode 
            // to preserve order on the subscription)
            lock (_callbackLock)
            {
                // The key is the stream id (channel)
                var message = RedisMessage.FromBytes(data);

                OnReceived(0, message.Id, message.ScaleoutMessage);
            }
        }

        private void ConnectWithRetry()
        {
            Task connectTask = ConnectToRedis();

            connectTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _trace.TraceError("Error connecting to Redis - " + task.Exception.GetBaseException());

                    if (_state == State.Disposing)
                    {
                        Shutdown();
                        return;
                    }

                    TaskAsyncHelper.Delay(ReconnectDelay)
                                   .Then(bus => bus.ConnectWithRetry(), this);
                }
                else
                {
                    var oldState = Interlocked.CompareExchange(ref _state,
                                                               State.Connected,
                                                               State.Closed);
                    if (oldState == State.Closed)
                    {
                        Open(0);
                    }
                    else if (oldState == State.Disposing)
                    {
                        Shutdown();
                    }
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are caught")]
        private Task ConnectToRedis()
        {
            if (_connection != null)
            {
                _connection.Closed -= OnConnectionClosed;
                _connection.Error -= OnConnectionError;
                _connection.Dispose();
                _connection = null;
            }

            // Create a new connection to redis with the factory
            RedisConnection connection = _connectionFactory();

            connection.Closed += OnConnectionClosed;
            connection.Error += OnConnectionError;

            try
            {
                _trace.TraceInformation("Connecting...");

                // Start the connection
                return connection.Open().Then(() =>
                {
                    _trace.TraceInformation("Connection opened");

                    // Create a subscription channel in redis
                    RedisSubscriberConnection channel = connection.GetOpenSubscriberChannel();
                    channel.CompletionMode = ResultCompletionMode.PreserveOrder;

                    // Subscribe to the registered connections
                    return channel.Subscribe(_key, OnMessage).Then(() =>
                    {
                        _trace.TraceVerbose("Subscribed to event " + _key);

                        _channel = channel;
                        _connection = connection;
                    });
                });
            }
            catch (Exception ex)
            {
                _trace.TraceError("Error connecting to Redis - " + ex.GetBaseException());

                return TaskAsyncHelper.FromError(ex);
            }
        }

        private static class State
        {
            public const int Closed = 0;
            public const int Connected = 1;
            public const int Disposing = 2;
            public const int Disposed = 3;
        }
    }
}
