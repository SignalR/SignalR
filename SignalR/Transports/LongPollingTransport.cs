using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Hosting;
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
            : base(context, heartBeat)
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

        public IEnumerable<string> Groups
        {
            get
            {
                if (IsConnectRequest)
                {
                    return Enumerable.Empty<string>();
                }

                string groupValue = Context.Request.QueryString["groups"];

                if (String.IsNullOrEmpty(groupValue))
                {
                    return Enumerable.Empty<string>();
                }

                return _jsonSerializer.Parse<string[]>(groupValue);
            }
        }

        private bool IsConnectRequest
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

        public Func<string, Task> Received { get; set; }

        public Func<Task> TransportConnected { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        public override Func<Task> Disconnected { get; set; }

        public Func<Exception, Task> Error { get; set; }

        public Task ProcessRequest(ITransportConnection connection)
        {
            Connection = connection;

            if (IsSendRequest)
            {
                return ProcessSendRequest();
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
                        return TaskAsyncHelper.Interleave(ProcessReceiveRequest, Reconnected, connection);
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
            HeartBeat.AddConnection(this);

            if (Connected != null)
            {
                // Return a task that completes when the connected event task & the receive loop task are both finished
                return TaskAsyncHelper.Interleave(ProcessReceiveRequest, Connected, connection);
            }

            return ProcessReceiveRequest(connection);
        }

        private Task ProcessReceiveRequest(ITransportConnection connection, Action postReceive = null)
        {
            HeartBeat.UpdateConnection(this);
            HeartBeat.MarkConnection(this);

            if (TransportConnected != null)
            {
                TransportConnected().Catch();
            }

            // ReceiveAsync() will async wait until a message arrives then return
            var receiveTask = IsConnectRequest ?
                              connection.ReceiveAsync(TimeoutToken) :
                              connection.ReceiveAsync(MessageId, TimeoutToken);

            if (postReceive != null)
            {
                postReceive();
            }

            return receiveTask.Then(response => Send(response));
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