// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin
{
    public class ServerResponse : IResponse
    {
        private readonly CancellationToken _callCancelled;
        private readonly OwinResponse _response;
        private readonly Stream _responseBody;

        public ServerResponse(IDictionary<string, object> environment)
        {
            _response = new OwinResponse(environment);
            _callCancelled = _response.Get<CancellationToken>(OwinConstants.CallCancelled);
            _responseBody = _response.Body;
        }

        public CancellationToken CancellationToken
        {
            get { return _callCancelled; }
        }

        public int StatusCode
        {
            get { return _response.StatusCode; }
            set { _response.StatusCode = value; }
        }

        public string ContentType
        {
            get { return _response.ContentType; }
            set { _response.ContentType = value; }
        }

        public void Write(ArraySegment<byte> data)
        {
            _responseBody.Write(data.Array, data.Offset, data.Count);
        }

        public Task Flush()
        {
            return _responseBody.FlushAsync();
        }
    }
}
