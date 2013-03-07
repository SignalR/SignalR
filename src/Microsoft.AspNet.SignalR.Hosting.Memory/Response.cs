// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using IClientResponse = Microsoft.AspNet.SignalR.Client.Http.IResponse;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Calls to close with dispose the stream")]
    internal class Response : IClientResponse
    {
        private readonly ClientStream _stream;

        public Response(ClientStream stream)
        {
            _stream = stream;
        }

        public Stream GetStream()
        {
            return _stream;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
