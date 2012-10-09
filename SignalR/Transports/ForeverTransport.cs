using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public abstract class ForeverTransport : TransportDisconnectBase, ITransport
    {
        private readonly IPerformanceCounterManager _counters;
        private IJsonSerializer _jsonSerializer;
        private string _lastMessageId;

        private const int MaxMessages = 10;

        public ForeverTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartBeat>(),
                   resolver.Resolve<IPerformanceCounterManager>())
        {
        }

        public ForeverTransport(HostContext context,
                                IJsonSerializer jsonSerializer,
                                ITransportHeartBeat heartBeat,
                                IPerformanceCounterManager performanceCounterWriter)
            : base(context, jsonSerializer, heartBeat, performanceCounterWriter)
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
            HeartBeat.MarkConnection(this);

            if (Sending != null)
            {
                Sending(payload);
            }
        }

        protected virtual void OnSendingResponse(PersistentResponse response)
        {
            HeartBeat.MarkConnection(this);

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

            if (Context.Request.Url.LocalPath.EndsWith("/send"))
            {
                return ProcessSendRequest();
            }
            else if (IsAbortRequest)
            {
                return Connection.Abort(ConnectionId);
            }
            else
            {
                if (IsConnectRequest)
                {
                    if (Connected != null)
                    {
                        // Return a task that completes when the connected event task & the receive loop task are both finished
                        bool newConnection = HeartBeat.AddConnection(this);
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

            JsonSerializer.Serialize(value, OutputWriter);
            OutputWriter.Flush();

            return Context.Response.EndAsync();
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
            HeartBeat.AddConnection(this);
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
                    postReceive();
                }

                return InitializeResponse(connection);
            };

            return ProcessMessages(connection, afterReceive);
        }

        private Task ProcessMessages(ITransportConnection connection, Func<Task> postReceive = null)
        {
            var tcs = new TaskCompletionSource<object>();

            Action endRequest = () =>
            {
                tcs.TrySetResult(null);
                CompleteRequest();
            };

            ProcessMessages(connection, postReceive, endRequest);

            return tcs.Task;
        }

        private void ProcessMessages(ITransportConnection connection, Func<Task> postReceive, Action endRequest)
        {
            IDisposable subscription = null;
            var wh = new ManualResetEventSlim(initialState: false);

            // End the request if the connection end token is triggered
            CancellationTokenRegistration registration = ConnectionEndToken.Register(() =>
            {
                wh.Wait();
                subscription.Dispose();
            });

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
                        HeartBeat.RemoveConnection(this);

                        endRequest();

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

                    endRequest();

                    // Dispose everything
                    registration.Dispose();
                    subscription.Dispose();

                    return TaskAsyncHelper.False;
                }
                else
                {
                    return Send(response).Then(() => TaskAsyncHelper.True);
                }
            },
            MaxMessages);

            if (postReceive != null)
            {
                postReceive().Catch(_counters.ErrorsAllTotal, _counters.ErrorsAllPerSec)
                             .ContinueWith(task => wh.Set());
            }
            else
            {
                wh.Set();
            }
        }
    }
}