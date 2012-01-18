using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Abstractions;

namespace SignalR.Transports
{
    public abstract class TransportDisconnectBase : ITrackingDisconnect
    {
        private readonly HostContext _context;
        private readonly ITransportHeartBeat _heartBeat;
        
        protected int _isDisconnected;

        public TransportDisconnectBase(HostContext context, ITransportHeartBeat heartBeat)
        {
            _context = context;
            _heartBeat = heartBeat;
        }

        public string ConnectionId
        {
            get
            {
                return _context.Request.QueryString["connectionId"];
            }
        }

        public abstract Func<Task> Disconnected { get; set; }

        public bool IsAlive
        {
            get { return _context.Response.IsClientConnected; }
        }

        public virtual TimeSpan DisconnectThreshold
        {
            get { return TimeSpan.FromSeconds(5); }
        }

        public Task Disconnect()
        {
            if (Interlocked.Exchange(ref _isDisconnected, 1) == 0)
            {
                var disconnected = Disconnected; // copy before invoking event to avoid race
                if (disconnected != null)
                {
                    return disconnected()
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                // Observe & trace any exception
                                Trace.TraceError("SignalR: Error during ForeverTransport disconnect: {0}", t.Exception);
                            }
                            return SendDisconnectCommand();
                        })
                        .FastUnwrap();
                }
                else
                {
                    return SendDisconnectCommand();
                }
            }
            else
            {
                // somebody else already fired the Disconnect event
                return TaskAsyncHelper.Empty;
            }
        }

        protected IReceivingConnection Connection
        {
            get;
            set;
        }

        protected HostContext Context
        {
            get { return _context; }
        }

        protected ITransportHeartBeat HeartBeat
        {
            get { return _heartBeat; }
        }

        private Task SendDisconnectCommand()
        {
            var command = new SignalCommand
            {
                Type = CommandType.Disconnect,
                ExpiresAfter = TimeSpan.FromMinutes(30)
            };

            return Connection.SendCommand(command);
        }
    }
}