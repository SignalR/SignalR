using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gate;

namespace SignalR.Server
{
    class ServerResponse : IResponse
    {
        private readonly Response _res;
        private readonly CancellationToken _callCancelled;

        public ServerResponse(Response res, CancellationToken callCancelled)
        {
            _res = res;
            _callCancelled = callCancelled;
        }

        public bool IsClientConnected
        {
            get { return !_callCancelled.IsCancellationRequested; }
        }

        public string ContentType
        {
            get { return _res.ContentType; }
            set { _res.ContentType = value; }
        }

        public void Write(ArraySegment<byte> data)
        {
            _res.OutputStream.Write(data.Array, data.Offset, data.Count);
        }

        public Task FlushAsync()
        {
            _res.OutputStream.Flush();
            return TaskHelpers.Completed();
        }

        public Task EndAsync()
        {
            _res.OutputStream.Flush();
            return TaskHelpers.Completed();
        }

        public IDictionary<string, string[]> Headers
        {
            get { return _res.Headers; }
        }
    }
}
