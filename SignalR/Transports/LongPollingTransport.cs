using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Abstractions;

namespace SignalR.Transports
{
    public class LongPollingTransport : ITransport, ITrackingDisconnect
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HostContext _context;
        private readonly ITransportHeartBeat _heartBeat;
        private IReceivingConnection _connection;
        private bool _disconnected;

        public LongPollingTransport(HostContext context, IJsonSerializer jsonSerializer)
            : this(context, jsonSerializer, TransportHeartBeat.Instance)
        {

        }

        public LongPollingTransport(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
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

        public TimeSpan DisconnectThreshold
        {
            get { return TimeSpan.FromMilliseconds(LongPollDelay); }
        }

        public IEnumerable<string> Groups
        {
            get
            {
                string groupValue = _context.Request.QueryStringOrForm("groups");

                if (String.IsNullOrEmpty(groupValue))
                {
                    return Enumerable.Empty<string>();
                }

                return groupValue.Split(',');
            }
        }

        private bool IsConnectRequest
        {
            get
            {
                return _context.Request.Url.LocalPath.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool IsSendRequest
        {
            get
            {
                return _context.Request.Url.LocalPath.EndsWith("/send", StringComparison.OrdinalIgnoreCase);
            }
        }

        private long? MessageId
        {
            get
            {
                long messageId;
                if (Int64.TryParse(_context.Request.QueryString["messageId"], out messageId))
                {
                    return messageId;
                }
                return null;
            }
        }

        public string ConnectionId
        {
            get
            {
                return _context.Request.QueryString["connectionId"];
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

        public Func<Task> ProcessRequest(IReceivingConnection connection)
        {
            _connection = connection;

            if (IsSendRequest)
            {
                ProcessSendRequest();
            }
            else
            {
                if (IsConnectRequest)
                {
                    return ProcessConnectRequest(connection);
                }
                else if (MessageId != null)
                {
                    return ProcessReceiveRequest(connection);
                }
            }

            return null;
        }

        public virtual Task Send(PersistentResponse response)
        {
            _heartBeat.MarkConnection(this);

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
            _context.Response.ContentType = Json.MimeType;
            return _context.Response.WriteAsync(payload);
        }

        public virtual void Disconnect()
        {
            if (!_disconnected && Disconnected != null)
            {
                Disconnected();
            }

            _disconnected = true;
            
            // Force connection to close by sending a command signal
            _connection.SendCommand(
                new SignalCommand
                {
                    Type = CommandType.Disconnect,
                    ExpiresAfter = TimeSpan.FromMinutes(30)
                });
        }


        private void ProcessSendRequest()
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

        private Func<Task> ProcessConnectRequest(IReceivingConnection connection)
        {
            // Since this is the first request, there's no data we need to retrieve so just wait
            // on a message to come through
            _heartBeat.AddConnection(this);
            if (Connected != null)
            {
                Connected();
            }

            // ReceiveAsync() will async wait until a message arrives then return
            return () => connection.ReceiveAsync()
                .Then(new Func<PersistentResponse, Task>(response => Send(response)))
                .FastUnwrap();
        }

        private Func<Task> ProcessReceiveRequest(IReceivingConnection connection)
        {
            _heartBeat.AddConnection(this);
            // If there is a message id then we receive with that id, which will either return
            // immediately if there are already messages since that id, or wait until new
            // messages come in and then return
            return () => connection.ReceiveAsync(MessageId.Value)
                .Then(new Func<PersistentResponse, Task>(response => Send(response)))
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