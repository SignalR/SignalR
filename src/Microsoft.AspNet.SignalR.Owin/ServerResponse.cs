﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;

namespace Microsoft.AspNet.SignalR.Owin
{
    public partial class ServerResponse : IResponse
    {
        private readonly CancellationToken _callCancelled;
        private readonly IDictionary<string, object> _environment;
        private Stream _responseBody;

        public ServerResponse(IDictionary<string, object> environment)
        {
            _environment = environment;
            _callCancelled = _environment.Get<CancellationToken>(OwinConstants.CallCancelled);
        }

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
#if NET45
            return ResponseBody.FlushAsync();
#else
            return TaskAsyncHelper.FromMethod(() => ResponseBody.Flush());
#endif
        }

        public Task EndAsync()
        {
            return FlushAsync();
        }

        public IDictionary<string, string[]> ResponseHeaders
        {
            get { return _environment.Get<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders); }
        }

        public Stream ResponseBody
        {
            get
            {
                if (_responseBody == null)
                {
                    _responseBody = _environment.Get<Stream>(OwinConstants.ResponseBody);
                }

                return _responseBody;
            }
        }

        public Action DisableResponseBuffering
        {
            get { return _environment.Get<Action>(OwinConstants.DisableResponseBuffering) ?? (() => { }); }
        }
    }
}
