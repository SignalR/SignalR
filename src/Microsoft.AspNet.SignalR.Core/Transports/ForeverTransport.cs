﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    public abstract class ForeverTransport : TransportDisconnectBase, ITransport
    {
        private readonly IPerformanceCounterManager _counters;
        private IJsonSerializer _jsonSerializer;
        private string _lastMessageId;

        private const int MaxMessages = 10;

        protected ForeverTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>())
        {
        }

        protected ForeverTransport(HostContext context,
                                   IJsonSerializer jsonSerializer,
                                   ITransportHeartbeat heartbeat,
                                   IPerformanceCounterManager performanceCounterWriter)
            : base(context, jsonSerializer, heartbeat, performanceCounterWriter)
        {
            _jsonSerializer = jsonSerializer;
            _counters = performanceCounterWriter;
        }

        protected string LastMessageId
        {
            get
            {
                if (_lastMessageId == null)
                {
                    _lastMessageId = Context.Request.QueryString["messageId"];
                }

                return _lastMessageId;
            }
            private set
            {
                _lastMessageId = value;
            }
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        protected virtual void OnSending(string payload)
        {
            Heartbeat.MarkConnection(this);

            if (Sending != null)
            {
                Sending(payload);
            }
        }

        protected virtual void OnSendingResponse(PersistentResponse response)
        {
            Heartbeat.MarkConnection(this);

            if (SendingResponse != null)
            {
                SendingResponse(response);
            }
        }

        protected static void OnReceiving(string data)
        {
            if (Receiving != null)
            {
                Receiving(data);
            }
        }

        // Static events intended for use when measuring performance
        public static event Action<string> Sending;

        public static event Action<PersistentResponse> SendingResponse;

        public static event Action<string> Receiving;

        public Func<string, Task> Received { get; set; }

        public Func<Task> TransportConnected { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        protected Task ProcessRequestCore(ITransportConnection connection)
        {
            Connection = connection;

            if (Context.Request.Url.LocalPath.EndsWith("/send", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessSendRequest();
            }
            else if (IsAbortRequest)
            {
                return Connection.Abort(ConnectionId);
            }
            else
            {
                InitializePersistentState();

                if (IsConnectRequest)
                {
                    if (Connected != null)
                    {
                        // Return a task that completes when the connected event task & the receive loop task are both finished
                        bool newConnection = Heartbeat.AddConnection(this);
                        return TaskAsyncHelper.Interleave(ProcessReceiveRequestWithoutTracking, () =>
                        {
                            if (newConnection)
                            {
                                return Connected().Then(() => _counters.ConnectionsConnected.Increment());
                            }
                            return TaskAsyncHelper.Empty;
                        }
                        , connection, Completed);
                    }

                    return ProcessReceiveRequest(connection);
                }

                if (Reconnected != null)
                {
                    // Return a task that completes when the reconnected event task & the receive loop task are both finished
                    Func<Task> reconnected = () => Reconnected().Then(() => _counters.ConnectionsReconnected.Increment());
                    return TaskAsyncHelper.Interleave(ProcessReceiveRequest, reconnected, connection, Completed);
                }

                return ProcessReceiveRequest(connection);
            }
        }

        public virtual Task ProcessRequest(ITransportConnection connection)
        {
            return ProcessRequestCore(connection);
        }

        public abstract Task Send(PersistentResponse response);

        public virtual Task Send(object value)
        {
            Context.Response.ContentType = Json.MimeType;

            return EnqueueOperation(() =>
            {
                JsonSerializer.Serialize(value, OutputWriter);
                OutputWriter.Flush();

                return Context.Response.EndAsync().Catch(IncrementErrorCounters);
            });
        }

        protected virtual Task InitializeResponse(ITransportConnection connection)
        {
            return TaskAsyncHelper.Empty;
        }

        protected void IncrementErrorCounters(Exception exception)
        {
            _counters.ErrorsTransportTotal.Increment();
            _counters.ErrorsTransportPerSec.Increment();
            _counters.ErrorsAllTotal.Increment();
            _counters.ErrorsAllPerSec.Increment();
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"];

            OnReceiving(data);

            if (Received != null)
            {
                return Received(data);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessReceiveRequest(ITransportConnection connection, Action postReceive = null)
        {
            Heartbeat.AddConnection(this);
            return ProcessReceiveRequestWithoutTracking(connection, postReceive);
        }

        private Task ProcessReceiveRequestWithoutTracking(ITransportConnection connection, Action postReceive = null)
        {
            Func<Task> afterReceive = () =>
            {
                if (TransportConnected != null)
                {
                    TransportConnected().Catch(_counters.ErrorsAllTotal, _counters.ErrorsAllPerSec);
                }

                if (postReceive != null)
                {
                    try
                    {
                        postReceive();
                    }
                    catch (Exception ex)
                    {
                        return TaskAsyncHelper.FromError(ex);
                    }
                }

                return InitializeResponse(connection);
            };

            return ProcessMessages(connection, afterReceive);
        }

        private Task ProcessMessages(ITransportConnection connection, Func<Task> postReceive = null)
        {
            var tcs = new TaskCompletionSource<object>();

            Action<Exception> endRequest = (ex) =>
            {
                if (ex != null)
                {
                    tcs.TrySetException(ex);
                }
                else
                {
                    tcs.TrySetResult(null);
                }

                CompleteRequest();
            };

            ProcessMessages(connection, postReceive, endRequest);

            return tcs.Task;
        }

        private void ProcessMessages(ITransportConnection connection, Func<Task> postReceive, Action<Exception> endRequest)
        {
            IDisposable subscription = null;
            var wh = new ManualResetEventSlim(initialState: false);
            var registration = default(CancellationTokenRegistration);

            try
            {
                subscription = connection.Receive(LastMessageId, response =>
                {
                    // We need to wait until post receive has been called
                    wh.Wait();

                    response.TimedOut = IsTimedOut;

                    // If we're telling the client to disconnect then clean up the instantiated connection.
                    if (response.Disconnect)
                    {
                        // Send the response before removing any connection data
                        return Send(response).Then(() =>
                        {
                            // Remove connection without triggering disconnect
                            Heartbeat.RemoveConnection(this);

                            endRequest(null);

                            // Dispose everything
                            registration.Dispose();
                            subscription.Dispose();

                            return TaskAsyncHelper.False;
                        });
                    }
                    else if (response.TimedOut ||
                             response.Aborted ||
                             ConnectionEndToken.IsCancellationRequested)
                    {
                        if (response.Aborted)
                        {
                            // If this was a clean disconnect raise the event.
                            OnDisconnect();
                        }

                        endRequest(null);

                        // Dispose everything
                        registration.Dispose();
                        subscription.Dispose();

                        return TaskAsyncHelper.False;
                    }
                    else
                    {
                        return Send(response).Then(() => TaskAsyncHelper.True)
                                             .Catch(IncrementErrorCounters)
                                             .Catch(ex => End());
                    }
                },
                MaxMessages);
            }
            catch (Exception ex)
            {
                endRequest(ex);

                wh.Set();

                return;
            }

            if (postReceive != null)
            {
                postReceive().Catch(_counters.ErrorsAllTotal, _counters.ErrorsAllPerSec)
                             .Catch(ex => endRequest(ex))
                             .ContinueWith(task => wh.Set());
            }
            else
            {
                wh.Set();
            }

            try
            {
                // End the request if the connection end token is triggered
                registration = ConnectionEndToken.Register(state =>
                {
                    // This is only null if we failed to create the subscription
                    ((IDisposable)state).Dispose();
                },
                subscription);
            }
            catch (ObjectDisposedException)
            {
                // If we've ended the connection before we got a chance to register the connection
                // then dispose the subscription immediately
                subscription.Dispose();
            }
        }
    }
}
