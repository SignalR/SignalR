﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public abstract class TransportDisconnectBase : ITrackingConnection
    {
        private readonly HostContext _context;
        private readonly ITransportHeartBeat _heartBeat;
        private readonly IJsonSerializer _jsonSerializer;

        protected int _isDisconnected;
        private readonly CancellationTokenSource _timeoutTokenSource;

        public TransportDisconnectBase(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
        {
            _context = context;
            _jsonSerializer = jsonSerializer;
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

        public Func<Task> Disconnected { get; set; }

        public virtual bool IsAlive
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

        protected virtual bool IsConnectRequest
        {
            get
            {
                return true;
            }
        }

        protected bool IsAbortRequest
        {
            get
            {
                return Context.Request.Url.LocalPath.EndsWith("/abort", StringComparison.OrdinalIgnoreCase);
            }
        }

        public Task Disconnect()
        {
            return OnDisconnect().Then(() => Connection.Close());
        }

        public Task OnDisconnect()
        {
            // When a connection is aborted (graceful disconnect) we send a command to it
            // telling to to disconnect. At that moment, we raise the disconnect event and
            // remove this connection from the heartbeat so we don't end up raising it for the same connection.
            HeartBeat.RemoveConnection(this);
            if (Interlocked.Exchange(ref _isDisconnected, 1) == 0)
            {
                var disconnected = Disconnected; // copy before invoking event to avoid race
                if (disconnected != null)
                {
                    Debug.WriteLine("TransportDisconnectBase: Disconnect fired for connection {0}", (object)ConnectionId);
                    return disconnected().Catch();
                }
            }

            return TaskAsyncHelper.Empty;
        }

        public void Timeout()
        {
            _timeoutTokenSource.Cancel();
        }

        public virtual void KeepAlive()
        {
        }

        protected ITransportConnection Connection { get; set; }

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