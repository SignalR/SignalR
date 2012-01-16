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
                string groupValue = _context.Request.QueryString["groups"];

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

        private string MessageId
        {
            get
            {
                return _context.Request.QueryString["messageId"];
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

        public Func<string, Task> Received { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Disconnected { get; set; }

        public Func<Exception, Task> Error { get; set; }

        public Task ProcessRequest(IReceivingConnection connection)
        {
            _connection = connection;

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
            return _context.Response.EndAsync(payload);
        }

        public virtual Task Disconnect()
        {
            if (!_disconnected && Disconnected != null)
            {
                return Disconnected().Then(() => SendDisconnectCommand()).FastUnwrap();
            }

            return SendDisconnectCommand();
        }

        private Task SendDisconnectCommand()
        {
            _disconnected = true;

            var command = new SignalCommand
            {
                Type = CommandType.Disconnect,
                ExpiresAfter = TimeSpan.FromMinutes(30)
            };

            // Force connection to close by sending a command signal
            return _connection.SendCommand(command);
        }


        private Task ProcessSendRequest()
        {
            string data = _context.Request.Form["data"];

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
            _heartBeat.AddConnection(this);

            // ReceiveAsync() will async wait until a message arrives then return
            var receiveTask = IsConnectRequest ?
                              connection.ReceiveAsync() :
                              connection.ReceiveAsync(MessageId);

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