using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly CancellationTokenSource _endTokenSource;
        private readonly CancellationToken _hostShutdownToken;
        private readonly CancellationTokenSource _connectionEndToken;
        private readonly CancellationTokenSource _disconnectedToken;
        private string _connectionId;

        // Allocate some buffer space to write to the connection with
        private byte[] _buffer = new byte[4096];

        public TransportDisconnectBase(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
        {
            _context = context;
            _jsonSerializer = jsonSerializer;
            _heartBeat = heartBeat;
            _timeoutTokenSource = new CancellationTokenSource();
            _endTokenSource = new CancellationTokenSource();
            _disconnectedToken = new CancellationTokenSource();
            _hostShutdownToken = context.HostShutdownToken();
            Completed = new TaskCompletionSource<object>();

            // Create a token that represents the end of this connection's life
            _connectionEndToken = CancellationTokenSource.CreateLinkedTokenSource(_timeoutTokenSource.Token, _endTokenSource.Token, _disconnectedToken.Token, _hostShutdownToken);
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
                return _connectionEndToken.Token;
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
            _disconnectedToken.Cancel();

            if (Interlocked.Exchange(ref _isDisconnected, 1) == 0)
            {
                var disconnected = Disconnected; // copy before invoking event to avoid race
                if (disconnected != null)
                {
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

        public void End()
        {
            _endTokenSource.Cancel();
        }

        public void CompleteRequest()
        {
            Completed.TrySetResult(null);
        }

        protected Task WriteAsync(string data)
        {
            int count = Encoding.UTF8.GetByteCount(data);

            // Make sure the buffer is big enough
            while (count >= _buffer.Length)
            {
                Array.Resize(ref _buffer, count + 1);
            }

            Encoding.UTF8.GetBytes(data, 0, data.Length, _buffer, 0);

            return Context.Response.WriteAsync(new ArraySegment<byte>(_buffer, 0, count));
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