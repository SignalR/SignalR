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

        private bool IsSendRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/send", StringComparison.OrdinalIgnoreCase);
            }
        }

        private ulong? MessageId
        {
            get
            {
                ulong id;
                if (UInt64.TryParse(Context.Request.QueryString["messageId"], out id))
                {
                    return id;
                }
                return null;
            }
        }

        public Func<string, Task> Received { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        public override Func<Task> Disconnected { get; set; }

        public Func<Exception, Task> Error { get; set; }

        public Task ProcessRequest(IReceivingConnection connection)
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
                    if (Reconnected != null)
                    {
                        return Reconnected().Then(() => ProcessReceiveRequest(connection)).FastUnwrap();
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
            if (Sending != null)
            {
                Sending(payload);
            }

            Context.Response.ContentType = Json.MimeType;
            return Context.Response.EndAsync(payload);
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"];

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

        private Task ProcessConnectRequest(IReceivingConnection connection)
        {
            if (Connected != null)
            {
                return Connected().Then(() => ProcessReceiveRequest(connection)).FastUnwrap();
            }

            return ProcessReceiveRequest(connection);
        }

        private Task ProcessReceiveRequest(IReceivingConnection connection)
        {
            HeartBeat.AddConnection(this);

            // ReceiveAsync() will async wait until a message arrives then return
            var receiveTask = IsConnectRequest ?
                              connection.ReceiveAsync() :
                              connection.ReceiveAsync(MessageId.Value);

            return receiveTask.Then(new Func<PersistentResponse, Task>(response => Send(response)))
                              .FastUnwrap();
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