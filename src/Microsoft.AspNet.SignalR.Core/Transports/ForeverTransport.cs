// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;

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
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>())
        {
        }

        protected ForeverTransport(HostContext context,
                                   IJsonSerializer jsonSerializer,
                                   ITransportHeartbeat heartbeat,
                                   IPerformanceCounterManager performanceCounterWriter,
                                   ITraceManager traceManager)
            : base(context, jsonSerializer, heartbeat, performanceCounterWriter, traceManager)
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
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        internal TaskCompletionSource<object> InitializeTcs { get; set; }

        protected virtual void OnSending(string payload)
        {
            Heartbeat.MarkConnection(this);
        }

        protected virtual void OnSendingResponse(PersistentResponse response)
        {
            Heartbeat.MarkConnection(this);
        }

        public Func<string, Task> Received { get; set; }

        public Func<Task> TransportConnected { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        // Unit testing hooks
        internal Action AfterReceive;
        internal Action BeforeCancellationTokenCallbackRegistered;
        internal Action BeforeReceive;
        internal Action AfterRequestEnd;

        protected override void InitializePersistentState()
        {
            // PersistentConnection.OnConnectedAsync must complete before we can write to the output stream,
            // so clients don't indicate the connection has started too early.
            InitializeTcs = new TaskCompletionSource<object>();
            WriteQueue = new TaskQueue(InitializeTcs.Task);

            base.InitializePersistentState();
        }

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

                        // The connected callback
                        Func<Task> connected = () =>
                        {
                            if (newConnection)
                            {
                                return Connected().Then(() => _counters.ConnectionsConnected.Increment());
                            }
                            return TaskAsyncHelper.Empty;
                        };

                        return TaskAsyncHelper.Interleave(ProcessReceiveRequestWithoutTracking, connected, connection, Completed);
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
            Context.Response.ContentType = JsonUtility.MimeType;

            return EnqueueOperation(() =>
            {
                JsonSerializer.Serialize(value, OutputWriter);
                OutputWriter.Flush();

                return Context.Response.End().Catch(IncrementErrorCounters);
            });
        }

        protected virtual Task InitializeResponse(ITransportConnection connection)
        {
            return TaskAsyncHelper.Empty;
        }

        protected internal override Task EnqueueOperation(Func<Task> writeAsync)
        {
            Task task = base.EnqueueOperation(writeAsync);

            // If PersistentConnection.OnConnected has not completed (as indicated by InitializeTcs),
            // the queue will be blocked to prevent clients from prematurely indicating the connection has
            // started, but we must keep receive loop running to continue processing commands and to
            // prevent deadlocks caused by waiting on ACKs.
            if (InitializeTcs == null || InitializeTcs.Task.IsCompleted)
            {
                return task;
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"];

            if (Received != null)
            {
                return Received(data);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessReceiveRequest(ITransportConnection connection, Func<Task> postReceive = null)
        {
            Heartbeat.AddConnection(this);
            return ProcessReceiveRequestWithoutTracking(connection, postReceive);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller.")]
        private Task ProcessReceiveRequestWithoutTracking(ITransportConnection connection, Func<Task> postReceive = null)
        {
            Func<Task> afterReceive = () =>
            {
                return TaskAsyncHelper.Series(OnTransportConnected,
                                              () => InitializeResponse(connection),
                                              () =>
                                              {
                                                  if (postReceive != null)
                                                  {
                                                      return postReceive();
                                                  }
                                                  return TaskAsyncHelper.Empty;
                                              });
            };

            return ProcessMessages(connection, afterReceive);
        }

        private Task OnTransportConnected()
        {
            if (TransportConnected != null)
            {
                return TransportConnected().Catch(_counters.ErrorsAllTotal, _counters.ErrorsAllPerSec);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessMessages(ITransportConnection connection, Func<Task> postReceive)
        {
            var tcs = new TaskCompletionSource<object>();

            Action<Exception> endRequest = (ex) =>
            {
                Trace.TraceInformation("DrainWrites(" + ConnectionId + ")");

                // Drain the task queue for pending write operations so we don't end the request and then try to write
                // to a corrupted request object.
                WriteQueue.Drain().Catch().ContinueWith(task =>
                {
                    if (ex != null)
                    {
                        tcs.TrySetUnwrappedException(ex);
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }

                    CompleteRequest();

                    Trace.TraceInformation("EndRequest(" + ConnectionId + ")");
                },
                TaskContinuationOptions.ExecuteSynchronously);

                if (AfterRequestEnd != null)
                {
                    AfterRequestEnd();
                }
            };

            ProcessMessages(connection, postReceive, endRequest);

            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This will be cleaned up later.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed to the caller.")]
        private void ProcessMessages(ITransportConnection connection, Func<Task> postReceive, Action<Exception> endRequest)
        {
            IDisposable subscription = null;
            IDisposable registration = null;

            if (BeforeReceive != null)
            {
                BeforeReceive();
            }

            try
            {
                subscription = connection.Receive(LastMessageId, response =>
                {
                    response.TimedOut = IsTimedOut;

                    // If we're telling the client to disconnect then clean up the instantiated connection.
                    if (response.Disconnect)
                    {
                        // Send the response before removing any connection data
                        return Send(response).Then(() =>
                        {
                            registration.Dispose();

                            // Remove connection without triggering disconnect
                            Heartbeat.RemoveConnection(this);

                            endRequest(null);

                            return TaskAsyncHelper.False;
                        });
                    }
                    else if (response.TimedOut ||
                             response.Aborted ||
                             ConnectionEndToken.IsCancellationRequested)
                    {
                        // If this is null it's because the cancellation token tripped
                        // before we setup the registration at all.
                        if (registration != null)
                        {
                            registration.Dispose();
                        }

                        if (response.Aborted)
                        {
                            // If this was a clean disconnect raise the event.
                            OnDisconnect();
                        }

                        endRequest(null);

                        return TaskAsyncHelper.False;
                    }
                    else
                    {
                        return Send(response).Then(() => TaskAsyncHelper.True)
                                             .Catch(IncrementErrorCounters)
                                             .Catch(ex =>
                                             {
                                                 Trace.TraceInformation("Send failed for {0} with: {1}", ConnectionId, ex.GetBaseException());
                                             });
                    }
                },
                MaxMessages);
            }
            catch (Exception ex)
            {
                // Set the tcs so that the task queue isn't waiting forever
                InitializeTcs.TrySetResult(null);

                endRequest(ex);

                return;
            }

            if (AfterReceive != null)
            {
                AfterReceive();
            }

            postReceive().Catch(_counters.ErrorsAllTotal, _counters.ErrorsAllPerSec)
                         .Catch(ex => endRequest(ex))
                         .Catch(ex =>
                         {
                             Trace.TraceInformation("Failed post receive for {0} with: {1}", ConnectionId, ex.GetBaseException());
                         })
                         .ContinueWith(InitializeTcs);

            if (BeforeCancellationTokenCallbackRegistered != null)
            {
                BeforeCancellationTokenCallbackRegistered();
            }

            // This has to be done last incase it runs synchronously.
            registration = ConnectionEndToken.SafeRegister(state =>
            {
                Trace.TraceInformation("Cancel(" + ConnectionId + ")");

                state.Dispose();
            },
            subscription);
        }
    }
}
