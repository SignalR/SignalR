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
                if (Connected != null)
                {
                    Connected();
                }

                // Don't timeout and never buffer any output
                connection.ReceiveTimeout = TimeSpan.FromTicks(Int32.MaxValue - 1);
                _context.Response.BufferOutput = false;
                _context.Response.Buffer = false;
                return () => ProcessMessages(connection);
            }

            return null;
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

        public void Send(object value)
        {
            var payload = _jsonSerializer.Stringify(value);
            if (Sending != null)
            {
                Sending(payload);
            }
            _context.Response.Write(payload);
        }
    }
}