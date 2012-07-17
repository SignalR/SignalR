using System.Threading.Tasks;

namespace SignalR.Transports
{
    public class WebSocketTransport : ForeverTransport
    {
        private readonly HostContext _context;
        private IWebSocket _socket;
        private bool _isAlive = true;

        public WebSocketTransport(HostContext context,
                                  IDependencyResolver resolver)
            : this(context, 
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartBeat>())
        {
        }

        public WebSocketTransport(HostContext context, 
                                  IJsonSerializer serializer, 
                                  ITransportHeartBeat heartBeat)
            : base(context, serializer, heartBeat)
        {
            _context = context;
        }

        public override bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        public override Task ProcessRequest(ITransportConnection connection)
        {
            return _context.Request.AcceptWebSocketRequest(socket =>
            {
                _socket = socket;

                socket.OnClose = () =>
                {
                    OnDisconnect();

                    _isAlive = false;
                };

                socket.OnMessage = message =>
                {
                    OnReceiving(message);

                    if (Received != null)
                    {
                        Received(message).Catch();
                    }
                };

                socket.OnError = ex =>
                {
                    if (Error != null)
                    {
                        Error(ex).Catch();
                    }
                };

                return ProcessRequestCore(connection);
            });
        }

        public override Task Send(object value)
        {
            var data = JsonSerializer.Stringify(value);

            OnSending(data);

            return _socket.Send(data);
        }

        public override Task Send(PersistentResponse response)
        {
            var data = JsonSerializer.Stringify(response);

            OnSending(data);

            return _socket.Send(data);
        }
    }
}
