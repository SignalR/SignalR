﻿using System;
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
        private readonly CancellationTokenSource _timeoutTokenSource;

        public TransportDisconnectBase(HostContext context, ITransportHeartBeat heartBeat)
        {
            _context = context;
            _heartBeat = heartBeat;
            _timeoutTokenSource = new CancellationTokenSource();
            
            // Register the callback to cancel this connection
            var hostShutdownToken = context.HostShutdownToken();
            if (hostShutdownToken != CancellationToken.None)
            {
                hostShutdownToken.Register(_timeoutTokenSource.Cancel);
            }
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

        public CancellationToken TimeoutToken
        {
            get
            {
                return _timeoutTokenSource.Token;
            }
        }

        protected bool IsDisconnected
        {
            get
            {
                return _isDisconnected == 1;
            }
        }

        public bool IsTimedOut
        {
            get
            {
                return _timeoutTokenSource.IsCancellationRequested;
            }
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

        public void Timeout()
        {
            _timeoutTokenSource.Cancel();
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