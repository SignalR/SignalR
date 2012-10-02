using SignalR.Infrastructure;
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
                   resolver.Resolve<ITransportHeartBeat>(),
                   resolver.Resolve<IPerformanceCounterManager>())
        {
        }

        public WebSocketTransport(HostContext context, 
                                  IJsonSerializer serializer, 
                                  ITransportHeartBeat heartBeat,
                                  IPerformanceCounterManager performanceCounterWriter)
            : base(context, serializer, heartBeat, performanceCounterWriter)
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

        public override Task KeepAlive()
        {
            return this.Send(new object());
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

                socket.OnUngracefulClose = () =>
                {
                    // Die but let the heartbeat clean up the connection
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

                return ProcessRequestCore(connection);
            });
        }

        public override Task Send(object value)
        {
            var data = JsonSerializer.Stringify(value);

            OnSending(data);

            return _socket.Send(data).Catch(IncrementErrorCounters);
        }

        public override Task Send(PersistentResponse response)
        {
            var data = JsonSerializer.Stringify(response);

            OnSending(data);

            return _socket.Send(data).Catch(IncrementErrorCounters);
        }
    }
}
