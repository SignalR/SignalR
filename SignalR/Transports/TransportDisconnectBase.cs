using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting;

namespace SignalR.Transports
{
    public abstract class TransportDisconnectBase : ITrackingConnection
    {
        private readonly HostContext _context;
        private readonly ITransportHeartBeat _heartBeat;

        protected int _isDisconnected;
        protected int _isTimedout;

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
                            return Connection.Close();
                        })
                        .FastUnwrap();
                }
                else
                {
                    return Connection.Close();
                }
            }
            else
            {
                // somebody else already fired the Disconnect event
                return TaskAsyncHelper.Empty;
            }
        }

        public Task Timeout()
        {
            if (Interlocked.Exchange(ref _isTimedout, 1) == 0)
            {
                // Trigger the timeout event
                return Connection.Timeout().Catch();
            }
            return TaskAsyncHelper.Empty;
        }

        protected IReceivingConnection Connection { get; set; }

        protected HostContext Context
        {
            get { return _context; }
        }

        protected ITransportHeartBeat HeartBeat
        {
            get { return _heartBeat; }
        }
    }
}