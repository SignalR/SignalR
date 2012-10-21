// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    public abstract class TransportDisconnectBase : ITrackingConnection
    {
        private readonly HostContext _context;
        private readonly ITransportHeartBeat _heartBeat;
        private readonly IJsonSerializer _jsonSerializer;
        private TextWriter _outputWriter;

        protected int _isDisconnected;
        private readonly IPerformanceCounterManager _counters;
        private string _connectionId;
        private int _ended;

        // Token that represents a timeout
        private CancellationTokenSource _timeoutTokenSource;

        // Token that represents the end of the connection based on a combination of
        // conditions (timeout, disconnect, connection forcibly ended, host shutdown)
        private CancellationToken _connectionEndToken;
        private CancellationTokenSource _connectionEndTokenSource;

        // Token that represents an explocit call to End()
        private CancellationTokenSource _endTokenSource;

        // Token that represents a call to disconnect
        private CancellationTokenSource _disconnectedTokenSource;

        // Token that represents the host shutting down
        private CancellationToken _hostShutdownToken;

        public TransportDisconnectBase(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat, IPerformanceCounterManager performanceCounterManager)
        {
            _context = context;
            _jsonSerializer = jsonSerializer;
            _heartBeat = heartBeat;
            _counters = performanceCounterManager;
        }

        public string ConnectionId
        {
            get
            {
                if (_connectionId == null)
                {
                    _connectionId = _context.Request.QueryString["connectionId"];
                }

                return _connectionId;
            }
        }

        public TextWriter OutputWriter
        {
            get
            {
                if (_outputWriter == null)
                {
                    _outputWriter = new StreamWriter(Context.Response.AsStream(), Encoding.UTF8);
                    _outputWriter.NewLine = "\n";
                }

                return _outputWriter;
            }
        }

        protected TaskCompletionSource<object> Completed
        {
            get;
            private set;
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

        protected CancellationToken ConnectionEndToken
        {
            get
            {
                return _connectionEndToken;
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

        public virtual bool SupportsKeepAlive
        {
            get
            {
                return true;
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
                return Context.Request.Url.LocalPath.EndsWith("/connect", StringComparison.OrdinalIgnoreCase);
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
            return OnDisconnect().Then(() => Connection.Close(ConnectionId));
        }

        public Task OnDisconnect()
        {
            // When a connection is aborted (graceful disconnect) we send a command to it
            // telling to to disconnect. At that moment, we raise the disconnect event and
            // remove this connection from the heartbeat so we don't end up raising it for the same connection.
            HeartBeat.RemoveConnection(this);

            if (_disconnectedTokenSource != null)
            {
                _disconnectedTokenSource.Cancel();
            }

            if (Interlocked.Exchange(ref _isDisconnected, 1) == 0)
            {
                var disconnected = Disconnected; // copy before invoking event to avoid race
                if (disconnected != null)
                {
                    return disconnected().Catch()
                        .Then(() => _counters.ConnectionsDisconnected.Increment());
                }
            }

            return TaskAsyncHelper.Empty;
        }

        public void Timeout()
        {
            if (_timeoutTokenSource != null)
            {
                _timeoutTokenSource.Cancel();
            }
        }

        public virtual Task KeepAlive()
        {
            return TaskAsyncHelper.Empty;
        }

        public void End()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 0)
            {
                if (_endTokenSource != null)
                {
                    _endTokenSource.Cancel();
                }

                if (_connectionEndTokenSource != null)
                {
                    _connectionEndTokenSource.Dispose();
                }

                if (_timeoutTokenSource != null)
                {
                    _timeoutTokenSource.Dispose();
                }

                if (_disconnectedTokenSource != null)
                {
                    _disconnectedTokenSource.Dispose();
                }

                if (_endTokenSource != null)
                {
                    _endTokenSource.Dispose();
                }
            }
        }

        public void CompleteRequest()
        {
            if (Completed != null)
            {
                Completed.TrySetResult(null);
            }
        }

        protected void InitializePersistentState()
        {
            _timeoutTokenSource = new CancellationTokenSource();
            _endTokenSource = new CancellationTokenSource();
            _disconnectedTokenSource = new CancellationTokenSource();
            _hostShutdownToken = _context.HostShutdownToken();

            Completed = new TaskCompletionSource<object>();

            // Create a token that represents the end of this connection's life
            _connectionEndTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_timeoutTokenSource.Token, _endTokenSource.Token, _disconnectedTokenSource.Token, _hostShutdownToken);
            _connectionEndToken = _connectionEndTokenSource.Token;
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

        public Uri Url
        {
            get { return _context.Request.Url; }
        }
    }
}
