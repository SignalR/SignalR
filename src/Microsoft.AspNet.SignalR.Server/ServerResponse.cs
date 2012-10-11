using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Server.Infrastructure;

namespace Microsoft.AspNet.SignalR.Server
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
            try
            {
                ResponseBody.Flush();
            }
            catch
            {
            }

            return TaskAsyncHelper.Empty;
        }

        public Task EndAsync()
        {
            return FlushAsync();
        }
    }
}
