using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Server.Infrastructure;

namespace SignalR.Server
{
    public partial class ServerResponse : IResponse
    {
        private readonly CancellationToken _callCancelled;

        public bool IsClientConnected
        {
            get { return !_callCancelled.IsCancellationRequested; }
        }

        public string ContentType
        {
            get { return ResponseHeaders.GetHeader("Content-Type"); }
            set { ResponseHeaders.SetHeader("Content-Type", value); }
        }

        public void Write(ArraySegment<byte> data)
        {
            ResponseBody.Write(data.Array, data.Offset, data.Count);
        }

        public Task FlushAsync()
        {
            ResponseBody.Flush();
            return TaskAsyncHelper.Empty;
        }

        public Task EndAsync()
        {
            ResponseBody.Flush();
            return TaskAsyncHelper.Empty;
        }
    }
}
