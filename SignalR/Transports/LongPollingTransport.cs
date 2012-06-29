using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class LongPollingTransport : TransportDisconnectBase, ITransport
    {
        private IJsonSerializer _jsonSerializer;

        public LongPollingTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartBeat>())
        {

        }

        public LongPollingTransport(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
            : base(context, jsonSerializer, heartBeat)
        {
            _jsonSerializer = jsonSerializer;
        }

        // Static events intended for use when measuring performance
        public static event Action<string> Sending;
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

        public Func<Exception, Task> Error { get; set; }

        public Task ProcessRequest(ITransportConnection connection)
        {
            Connection = connection;

            if (IsSendRequest)
            {
                return ProcessSendRequest();
            }
            else if (IsAbortRequest)
            {
                return Connection.Abort();
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
                        return TaskAsyncHelper.Interleave(ProcessReceiveRequest, Reconnected, connection, Completed);
                    }

                    return ProcessReceiveRequest(connection);
                }
            }

            return null;
        }

        public virtual Task Send(PersistentResponse response)
        {
            HeartBeat.MarkConnection(this);

            AddTransportData(response);
            return Send((object)response);
        }

        public virtual Task Send(object value)
        {
            var payload = _jsonSerializer.Stringify(value);

            if (IsJsonp)
            {
                payload = Json.CreateJsonpCallback(JsonpCallback, payload);
            }

            if (Sending != null)
            {
                Sending(payload);
            }

            Context.Response.ContentType = IsJsonp ? Json.JsonpMimeType : Json.MimeType;
            return Context.Response.EndAsync(payload);
        }

        private Task ProcessSendRequest()
        {
            string data = IsJsonp ? Context.Request.QueryString["data"] : Context.Request.Form["data"];

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
                        return Connected();
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

            IDisposable subscription = null;
            var tcs = new TaskCompletionSource<object>();

            Action<Exception> end = (exception) =>
            {
                if (subscription != null)
                {
                    subscription.Dispose();
                }

                if (exception != null)
                {
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            };

            subscription = connection.Receive(MessageId, (ex, response) =>
            {
                if (ex != null)
                {
                    end(ex);
                    return TaskAsyncHelper.Empty;
                }

                response.TimedOut = IsTimedOut;

                if (response.Aborted)
                {
                    // If this was a clean disconnect then raise the event
                    OnDisconnect();
                }

                return Send(response).Then(cb => cb(null), end);
            });

            if (postReceive != null)
            {
                postReceive();
            }

            return tcs.Task;
        }

        private Task ProcessReceiveRequestWithoutTrackingOld(ITransportConnection connection, Action postReceive = null)
        {
            if (TransportConnected != null)
            {
                TransportConnected().Catch();
            }

            // ReceiveAsync() will async wait until a message arrives then return
            var receiveTask = IsConnectRequest ?
                              connection.ReceiveAsync(ConnectionEndToken) :
                              connection.ReceiveAsync(MessageId, ConnectionEndToken);

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

        private PersistentResponse AddTransportData(PersistentResponse response)
        {
            if (response != null)
            {
                response.TransportData["LongPollDelay"] = LongPollDelay;
            }

            return response;
        }
    }
}