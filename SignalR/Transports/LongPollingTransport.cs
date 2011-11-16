using System;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Transports
{
    public class LongPollingTransport : ITransport, ITrackingDisconnect
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HttpContextBase _context;
        private readonly ITransportHeartBeat _heartBeat;

        public LongPollingTransport(HttpContextBase context, IJsonSerializer jsonSerializer)
            : this(context, jsonSerializer, TransportHeartBeat.Instance)
        {

        }

        public LongPollingTransport(HttpContextBase context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
        {
            _context = context;
            _jsonSerializer = jsonSerializer;
            _heartBeat = heartBeat;
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

        private bool IsConnectRequest
        {
            get
            {
                return _context.Request.Path.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool IsSendRequest
        {
            get
            {
                return _context.Request.Path.EndsWith("/send", StringComparison.OrdinalIgnoreCase);
            }
        }

        private long? MessageId
        {
            get
            {
                long messageId;
                if (Int64.TryParse(_context.Request["messageId"], out messageId))
                {
                    return messageId;
                }
                return null;
            }
        }

        public string ClientId
        {
            get
            {
                return _context.Request["clientId"];
            }
        }

        public bool IsAlive
        {
            get
            {
                return _context.Response.IsClientConnected;
            }
        }

        public event Action<string> Received;

        public event Action Connected;

        public event Action Disconnected;

        public event Action<Exception> Error;

        public Func<Task> ProcessRequest(IConnection connection)
        {
            if (IsSendRequest)
            {
                if (Received != null || Receiving != null)
                {
                    string data = _context.Request.Form["data"];
                    if (Receiving != null)
                    {
                        Receiving(data);
                    }
                    if (Received != null)
                    {
                        Received(data);
                    }
                }
            }
            else
            {
                if (IsConnectRequest)
                {
                    // Since this is the first request, there's no data we need to retrieve so just wait
                    // on a message to come through
                    _heartBeat.AddConnection(this);
                    if (Connected != null)
                    {
                        Connected();
                    }

                    return () => connection.ReceiveAsync().ContinueWith(t =>
                    {
                        Send(t.Result);
                    });
                }
                else if (MessageId != null)
                {
                    _heartBeat.AddConnection(this);
                    // If there is a message id then we receive with that id, which will either return
                    // immediately if there are already messages since that id, or wait until new
                    // messages come in and then return
                    return () => connection.ReceiveAsync(MessageId.Value).ContinueWith(t =>
                    {
                        // Messages have arrived so let's return
                        Send(t.Result);
                        return TaskAsyncHelper.Empty;
                    }).Unwrap();
                }
            }

            return null;
        }

        public virtual void Send(PersistentResponse response)
        {
            _heartBeat.MarkConnection(this);

            AddTransportData(response);
            Send((object)response);
        }

        public virtual void Send(object value)
        {
            var payload = _jsonSerializer.Stringify(value);
            if (Sending != null)
            {
                Sending(payload);
            }
            _context.Response.ContentType = Json.MimeType;
            _context.Response.Write(payload);
        }

        public virtual void Disconnect()
        {
            if (Disconnected != null)
            {
                Disconnected();
            }
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