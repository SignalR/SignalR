// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable fields are disposed from a different method")]
    public abstract class TransportDisconnectBase : ITrackingConnection
    {
        private readonly HostContext _context;
        private readonly ITransportHeartbeat _heartbeat;
        private readonly IJsonSerializer _jsonSerializer;
        private TextWriter _outputWriter;

        private TraceSource _trace;

        private int _isDisconnected;
        private int _timedOut;
        private readonly IPerformanceCounterManager _counters;
        private string _connectionId;
        private int _ended;

        // Token that represents the end of the connection based on a combination of
        // conditions (timeout, disconnect, connection forcibly ended, host shutdown)
        private CancellationToken _connectionEndToken;
        private SafeCancellationTokenSource _connectionEndTokenSource;

        // Token that represents the host shutting down
        private CancellationToken _hostShutdownToken;
        private IDisposable _hostRegistration;

        // Queue to protect against overlapping writes to the underlying response stream
        private readonly TaskQueue _writeQueue = new TaskQueue();

        protected TransportDisconnectBase(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartbeat heartbeat, IPerformanceCounterManager performanceCounterManager, ITraceManager traceManager)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            if (heartbeat == null)
            {
                throw new ArgumentNullException("heartbeat");
            }

            if (performanceCounterManager == null)
            {
                throw new ArgumentNullException("performanceCounterManager");
            }

            if (traceManager == null)
            {
                throw new ArgumentNullException("traceManager");
            }

            _context = context;
            _jsonSerializer = jsonSerializer;
            _heartbeat = heartbeat;
            _counters = performanceCounterManager;

            _trace = traceManager["SignalR.Transports." + GetType().Name];
        }

        protected TraceSource Trace
        {
            get
            {
                return _trace;
            }
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

        public virtual TextWriter OutputWriter
        {
            get
            {
                if (_outputWriter == null)
                {
                    _outputWriter = new StreamWriter(Context.Response.AsStream(), new UTF8Encoding(false));
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

        public bool IsTimedOut
        {
            get
            {
                return _timedOut == 1;
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

        protected ITransportConnection Connection { get; set; }

        protected HostContext Context
        {
            get { return _context; }
        }

        protected ITransportHeartbeat Heartbeat
        {
            get { return _heartbeat; }
        }

        public Uri Url
        {
            get { return _context.Request.Url; }
        }

        public Task Disconnect()
        {
            return OnDisconnect().Then(() => Connection.Close(ConnectionId));
        }

        public Task OnDisconnect()
        {
            Trace.TraceInformation("OnDisconnect(" + ConnectionId + ")");

            // When a connection is aborted (graceful disconnect) we send a command to it
            // telling to to disconnect. At that moment, we raise the disconnect event and
            // remove this connection from the heartbeat so we don't end up raising it for the same connection.
            Heartbeat.RemoveConnection(this);
            
            if (Interlocked.Exchange(ref _isDisconnected, 1) == 0)
            {
                // End the connection
                End();

                var disconnected = Disconnected; // copy before invoking event to avoid race
                if (disconnected != null)
                {
                    return disconnected().Catch(ex =>
                    {
                        Trace.TraceInformation("Failed to raise disconnect: " + ex.GetBaseException());
                    })
                    .Then(() => _counters.ConnectionsDisconnected.Increment());
                }
            }

            return TaskAsyncHelper.Empty;
        }

        public void Timeout()
        {
            if (Interlocked.Exchange(ref _timedOut, 1) == 0)
            {
                Trace.TraceInformation("Timeout(" + ConnectionId + ")");
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
                Trace.TraceInformation("End(" + ConnectionId + ")");

                if (_connectionEndTokenSource != null)
                {
                    _connectionEndTokenSource.Cancel();
                }

                if (_connectionEndTokenSource != null)
                {
                    _connectionEndTokenSource.Dispose();
                }

                _hostRegistration.Dispose();
            }
        }

        public void CompleteRequest()
        {
            // REVIEW: We can get rid of this when we clean up the Interleave code.
            if (Completed != null)
            {
                Completed.TrySetResult(null);
            }
        }

        protected Task EnqueueOperation(Func<Task> writeAsync)
        {
            return _writeQueue.Enqueue(writeAsync);
        }

        protected void InitializePersistentState()
        {
            _hostShutdownToken = _context.HostShutdownToken();

            Completed = new TaskCompletionSource<object>();

            // Create a token that represents the end of this connection's life
            _connectionEndTokenSource = new SafeCancellationTokenSource();            
            _connectionEndToken = _connectionEndTokenSource.Token;

            // Handle the shutdown token's callback so we can end our token if it trips
            _hostRegistration = _hostShutdownToken.SafeRegister(state =>
            {
                state.Cancel();
            },
            _connectionEndTokenSource);
        }
    }
}
