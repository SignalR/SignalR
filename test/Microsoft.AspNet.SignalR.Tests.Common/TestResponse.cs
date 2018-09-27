using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class TestResponse : IResponse
    {
        private MemoryStream _body = new MemoryStream();

        public CancellationToken CancellationToken => CancellationTokenSource.Token;

        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public int StatusCode { get; set; }

        public string ContentType { get; set; }

        public byte[] GetBody() => _body.ToArray();

        public string GetBodyAsString() => Encoding.UTF8.GetString(GetBody());

        public Task Flush() => Task.CompletedTask;

        public void Write(ArraySegment<byte> data) => _body.Write(data.Array, data.Offset, data.Count);
    }
}
