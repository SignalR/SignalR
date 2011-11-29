using System;
using System.Threading.Tasks;
using System.Web;

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
            if (_context.Request.Path.EndsWith("/send"))
            {
                if (Received != null || Receiving != null)
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
            }
            else
            {
                if (IsConnectRequest && Connected != null)
                {
                    Connected();
                }

                InitializeResponse(connection);
                
                return () => ProcessMessages(connection);
            }

            return null;
        }

        protected virtual bool IsConnectRequest
        {
            get { return true; }
        }

        protected virtual void InitializeResponse(IConnection connection)
        {
            // Don't timeout and never buffer any output
            connection.ReceiveTimeout = TimeSpan.FromTicks(Int32.MaxValue - 1);
            Context.Response.BufferOutput = false;
            Context.Response.Buffer = false;
        }

        private Task ProcessMessages(IConnection connection)
        {
            if (_context.Response.IsClientConnected)
            {
                return connection.ReceiveAsync().ContinueWith(t =>
                {
                    Send(t.Result);
                    return ProcessMessages(connection);
                }).Unwrap();
            }

            if (Disconnected != null)
            {
                Disconnected();
            }
            return TaskAsyncHelper.Empty;
        }

        public virtual void Send(PersistentResponse response)
        {
            Send((object)response);
        }

        public virtual void Send(object value)
        {
            var payload = _jsonSerializer.Stringify(value);
            OnSending(payload);
            _context.Response.Write(payload);
        }
    }
}