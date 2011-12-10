using System;
using System.Threading.Tasks;
using System.Web;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class ForeverTransport : ITransport
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HttpContextBase _context;
        
        public ForeverTransport(HttpContextBase context, IJsonSerializer jsonSerializer)
        {
            _context = context;
            _jsonSerializer = jsonSerializer;
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        protected HttpContextBase Context
        {
            get { return _context; }
        }

        protected long? LastMessageId
        {
            get;
            set;
        }

        public string ConnectionId
        {
            get
            {
                return _context.Request.QueryString["connectionId"];
            }
        }

        protected virtual void OnSending(string payload)
        {
            if (Sending != null)
            {
                Sending(payload);
            }
        }

        // Static events intended for use when measuring performance
        public static event Action<string> Sending;
        public static event Action<string> Receiving;

        public event Action<string> Received;

        public event Action Connected;

        public event Action Disconnected;

        public event Action<Exception> Error;

        public Func<Task> ProcessRequest(IConnection connection)
        {
            if (Context.Request.Path.EndsWith("/send"))
            {
                string data = _context.Request.Form["data"];
                if (Receiving != null)
                {                    
                    Receiving(data);
                }
                if (Received != null)
                {
                    Received(data);
                }
            }
            else
            {
                if (IsConnectRequest && Connected != null)
                {
                    Connected();
                }

                return () => InitializeResponse(connection)
                    .Success( _ => ProcessMessages(connection, LastMessageId))
                    .FastUnwrap();
            }

            return null;
        }

        protected virtual bool IsConnectRequest
        {
            get { return true; }
        }

        protected virtual Task InitializeResponse(IConnection connection)
        {
            // Don't timeout
            connection.ReceiveTimeout = TimeSpan.FromDays(1);

            // This forces the IIS compression module to leave this response alone.
            // If we don't do this, it will buffer the response to suit its own compression
            // logic, resulting in partial messages being sent to the client.
            Context.Request.Headers.Remove("Accept-Encoding");

            Context.Response.Buffer = false;
            Context.Response.BufferOutput = false;
            Context.Response.CacheControl = "no-cache";
            Context.Response.AddHeader("Connection", "keep-alive");

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessMessages(IConnection connection, long? lastMessageId)
        {
            if (Context.Response.IsClientConnected)
            {
                var responseTask = lastMessageId == null
                    ? connection.ReceiveAsync()
                    : connection.ReceiveAsync(lastMessageId.Value);

                return responseTask.Success(t =>
                {
                    LastMessageId = t.Result.MessageId;
                    return Send(t.Result)
                        .Success(_ => ProcessMessages(connection, LastMessageId))
                        .FastUnwrap();
                }).FastUnwrap();
            }

            // Client is no longer connected
            if (Disconnected != null)
            {
                Disconnected();
            }
            return TaskAsyncHelper.Empty;
        }

        public virtual Task Send(PersistentResponse response)
        {
            return Send((object)response);
        }

        public virtual Task Send(object value)
        {
            var payload = _jsonSerializer.Stringify(value);
            OnSending(payload);
            return Context.Response.WriteAsync(payload);
        }
    }
}