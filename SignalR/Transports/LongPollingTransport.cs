using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Transports
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
                   resolver.Resolve<ITransportHeartBeat>(),
                   resolver.Resolve<IPerformanceCounterManager>())
        {

        }

        public LongPollingTransport(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat, IPerformanceCounterManager performanceCounterManager)
            : base(context, jsonSerializer, heartBeat, performanceCounterManager)
        {
            _jsonSerializer = jsonSerializer;
            _counters = performanceCounterManager;
        }

        // Static events intended for use when measuring performance
        public static event Action<string> Sending;
        public static event Action<PersistentResponse> SendingResponse;
        public static event Action<string> Receiving;

        /// <summary>
        /// The number of milliseconds to tell the browser to wait before restablishing a
        /// long poll connection after data is sent from the server. Defaults to 0.
        /// </summary>
        public static long LongPollDelay
        {
            get;
            set;
        }

        public override TimeSpan DisconnectThreshold
        {
            get { return TimeSpan.FromMilliseconds(LongPollDelay); }
        }

        protected override bool IsConnectRequest
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
                if (IsConnectRequest)
                {
                    return ProcessConnectRequest(connection);
                }
                else if (MessageId != null)
                {
                    if (IsReconnectRequest && Reconnected != null)
                    {
                        // Return a task that completes when the reconnected event task & the receive loop task are both finished
                        Func<Task> reconnected = () => Reconnected().Then(() => _counters.ConnectionsReconnected.Increment());
                        return TaskAsyncHelper.Interleave(ProcessReceiveRequest, reconnected, connection, Completed);
                    }

                    return ProcessReceiveRequest(connection);
                }
            }

            return null;
        }

        public virtual Task Send(PersistentResponse response)
        {
            HeartBeat.MarkConnection(this);

            if (SendingResponse != null)
            {
                SendingResponse(response);
            }

            AddTransportData(response);

            return Send((object)response);
        }

        public virtual Task Send(object value)
        {
            Context.Response.ContentType = IsJsonp ? Json.JsonpMimeType : Json.MimeType;

            if (IsJsonp)
            {
                OutputWriter.Write(JsonpCallback);
                OutputWriter.Write("(");
            }

            _jsonSerializer.Serialize(value, OutputWriter);

            if (IsJsonp)
            {
                OutputWriter.Write(");");
            }

            OutputWriter.Flush();
            return Context.Response.EndAsync();
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"] ?? Context.Request.QueryString["data"];

            if (Receiving != null)
            {
                Receiving(data);
            }

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
                bool newConnection = HeartBeat.AddConnection(this);

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

        private Task ProcessReceiveRequest(ITransportConnection connection, Action postReceive = null)
        {
            HeartBeat.AddConnection(this);
            return ProcessReceiveRequestWithoutTracking(connection, postReceive);
        }

        private Task ProcessReceiveRequestWithoutTracking(ITransportConnection connection, Action postReceive = null)
        {
            if (TransportConnected != null)
            {
                TransportConnected().Catch();
            }

            // ReceiveAsync() will async wait until a message arrives then return
            var receiveTask = IsConnectRequest ?
                              connection.ReceiveAsync(null, ConnectionEndToken, MaxMessages) :
                              connection.ReceiveAsync(MessageId, ConnectionEndToken, MaxMessages);

            if (postReceive != null)
            {
                postReceive();
            }

            return receiveTask.Then(response =>
            {
                response.TimedOut = IsTimedOut;

                if (response.Aborted)
                {
                    // If this was a clean disconnect then raise the event
                    OnDisconnect();
                }

                return Send(response);
            });
        }

        private void AddTransportData(PersistentResponse response)
        {
            if (LongPollDelay > 0)
            {
                if (response.TransportData == null)
                {
                    response.TransportData = new Dictionary<string, object>();
                }
                response.TransportData["LongPollDelay"] = LongPollDelay;
            }
        }
    }
}