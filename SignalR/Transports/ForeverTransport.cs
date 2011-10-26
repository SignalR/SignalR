using System;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Transports
{
    public class ForeverTransport : ITransport
    {
        private readonly IJsonStringifier _jsonStringifier;
        private readonly HttpContextBase _context;

        public ForeverTransport(HttpContextBase context, IJsonStringifier jsonStringifier)
        {
            _context = context;
            _jsonStringifier = jsonStringifier;
        }

        public event Action<string> Received;

        public event Action Connected;

        public event Action Disconnected;

        public event Action<Exception> Error;

        public Func<Task> ProcessRequest(IConnection connection)
        {
            if (_context.Request.Path.EndsWith("/send"))
            {
                string data = _context.Request["data"];
                if (Received != null)
                {
                    Received(data);
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
            _context.Response.Write(_jsonStringifier.Stringify(value));
        }
    }
}