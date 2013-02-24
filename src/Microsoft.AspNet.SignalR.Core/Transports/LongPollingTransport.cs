// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class LongPollingTransport : TransportDisconnectBase, ITransport
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IPerformanceCounterManager _counters;

        // This should be ok to do since long polling request never hang around too long
        // so we won't bloat memory
        private const int MaxMessages = 5000;

        public LongPollingTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>())
        {

        }

        public LongPollingTransport(HostContext context,
                                    IJsonSerializer jsonSerializer,
                                    ITransportHeartbeat heartbeat,
                                    IPerformanceCounterManager performanceCounterManager,
                                    ITraceManager traceManager)
            : base(context, heartbeat, performanceCounterManager, traceManager)
        {
            _jsonSerializer = jsonSerializer;
            _counters = performanceCounterManager;
        }

        /// <summary>
        /// The number of milliseconds to tell the browser to wait before restablishing a
        /// long poll connection after data is sent from the server. Defaults to 0.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "long", Justification = "Longpolling is a well known term")]
        public static long LongPollDelay
        {
            get;
            set;
        }

        public override TimeSpan DisconnectThreshold
        {
            get { return TimeSpan.FromMilliseconds(LongPollDelay); }
        }

        public override bool IsConnectRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool IsReconnectRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/reconnect", StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool IsJsonp
        {
            get
            {
                return !String.IsNullOrEmpty(JsonpCallback);
            }
        }

        private bool IsSendRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/send", StringComparison.OrdinalIgnoreCase);
            }
        }

        private string MessageId
        {
            get
            {
                return Context.Request.QueryString["messageId"];
            }
        }

        private string JsonpCallback
        {
            get
            {
                return Context.Request.QueryString["callback"];
            }
        }

        public override bool SupportsKeepAlive
        {
            get
            {
                return false;
            }
        }

        public Func<string, Task> Received { get; set; }

        public Func<Task> TransportConnected { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        public Task ProcessRequest(ITransportConnection connection)
        {
            Connection = connection;

            if (IsSendRequest)
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

                Task task = null;

                if (IsConnectRequest)
                {
                    task = ProcessConnectRequest(connection);
                }
                else if (MessageId != null)
                {
                    if (IsReconnectRequest && Reconnected != null)
                    {
                        // Return a task that completes when the reconnected event task & the receive loop task are both finished
                        Func<Task> reconnected = () => Reconnected().Then(() => _counters.ConnectionsReconnected.Increment());
                        task = TaskAsyncHelper.Interleave(ProcessReceiveRequest, reconnected, connection, Completed);
                    }
                    else
                    {
                        task = ProcessReceiveRequest(connection);
                    }
                }

                if (task != null)
                {
                    // Mark the request as completed once it's done
                    return task.Finally(state => CompleteRequest(), null);
                }
            }

            return null;
        }

        public Task Send(PersistentResponse response)
        {
            Heartbeat.MarkConnection(this);

            AddTransportData(response);

            return Send((object)response);
        }

        public Task Send(object value)
        {
            return EnqueueOperation(state =>
            {
                Context.Response.ContentType = IsJsonp ? JsonUtility.JavaScriptMimeType : JsonUtility.JsonMimeType;

                if (IsJsonp)
                {
                    OutputWriter.Write(JsonpCallback);
                    OutputWriter.Write("(");
                }

                _jsonSerializer.Serialize(state, OutputWriter);

                if (IsJsonp)
                {
                    OutputWriter.Write(");");
                }

                OutputWriter.Flush();
                return Context.Response.End()
                                       .Catch(IncrementErrors)
                                       .Catch(ex =>
                                       {
                                           Trace.TraceEvent(TraceEventType.Error, 0, "Failed EndAsync() for {0} with: {1}", ConnectionId, ex.GetBaseException());
                                       });
            },
            value);
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"] ?? Context.Request.QueryString["data"];

            if (Received != null)
            {
                return Received(data);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessConnectRequest(ITransportConnection connection)
        {
            if (Connected != null)
            {
                bool newConnection = Heartbeat.AddConnection(this);

                // Return a task that completes when the connected event task & the receive loop task are both finished
                return TaskAsyncHelper.Interleave(ProcessReceiveRequestWithoutTracking, () =>
                {
                    if (newConnection)
                    {
                        return Connected().Then(() => _counters.ConnectionsConnected.Increment());
                    }
                    return TaskAsyncHelper.Empty;
                },
                connection, Completed);
            }

            return ProcessReceiveRequest(connection);
        }

        private Task ProcessReceiveRequest(ITransportConnection connection, Func<Task> postReceive = null)
        {
            Heartbeat.AddConnection(this);
            return ProcessReceiveRequestWithoutTracking(connection, postReceive);
        }

        private Task ProcessReceiveRequestWithoutTracking(ITransportConnection connection, Func<Task> postReceive = null)
        {
            postReceive = postReceive ?? new Func<Task>(() => TaskAsyncHelper.Empty);

            Func<Task> afterReceive = () =>
            {
                var series = new Func<object, Task>[] 
                { 
                    OnTransportConnected,
                    state => ((Func<Task>)state).Invoke()
                };

                return TaskAsyncHelper.Series(series, new object[] { null, postReceive });
            };

            string messageId = IsConnectRequest ? null : MessageId;

            return Receive(connection, messageId, ConnectionEndToken, afterReceive);
        }

        private Task OnTransportConnected(object state)
        {
            if (TransportConnected != null)
            {
                return TransportConnected().Catch(_counters.ErrorsAllTotal, _counters.ErrorsAllPerSec);
            }

            return TaskAsyncHelper.Empty;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The subscription is disposed in the callback")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is captured in a task")]
        private Task Receive(ITransportConnection connection, string messageId, CancellationToken cancel, Func<Task> postReceive)
        {
            IDisposable subscription = null;
            int handled = 0;
            var disposer = new Disposer();

            IDisposable registration = cancel.SafeRegister(state =>
            {
                ((Disposer)state).Dispose();
            },
            disposer);

            try
            {
                var requestLifeTimeTcs = new TaskCompletionSource<object>();

                subscription = connection.Receive(messageId, (response, state) =>
                {
                    if (Interlocked.Exchange(ref handled, 1) == 1)
                    {
                        requestLifeTimeTcs.TrySetResult(null);

                        // Dispose of the cancellation token subscription
                        registration.Dispose();

                        return TaskAsyncHelper.False;
                    }

                    response.TimedOut = IsTimedOut;

                    if (response.Aborted)
                    {
                        // If this was a clean disconnect then raise the event
                        OnDisconnect();
                    }

                    // Send the response and return false
                    return Send(response).Then(() => TaskAsyncHelper.False);
                },
                MaxMessages,
                null);

                // Set the disposable
                disposer.Set(subscription);

                postReceive().Catch(ex => requestLifeTimeTcs.TrySetException(ex));

                return requestLifeTimeTcs.Task;
            }
            catch (Exception ex)
            {
                registration.Dispose();

                return TaskAsyncHelper.FromError(ex);
            }
        }

        private static void AddTransportData(PersistentResponse response)
        {
            if (LongPollDelay > 0)
            {
                response.LongPollDelay = LongPollDelay;
            }
        }
    }
}
