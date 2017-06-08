// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class FoundationHttpResponseMessageWrapper : IResponse, IDisposable
    {
        private readonly Windows.Web.Http.HttpResponseMessage _httpResponseMessage;

        public FoundationHttpResponseMessageWrapper(Windows.Web.Http.HttpResponseMessage httpResponseMessage)
        {
            _httpResponseMessage = httpResponseMessage;
        }

        public string ReadAsString()
        {
            return _httpResponseMessage.Content
                    .ReadAsStringAsync()
                    .GetResults();
        }

        public Stream GetStream()
        {
            return _httpResponseMessage.Content
                    .ReadAsInputStreamAsync()
                    .GetResults()
                    .AsStreamForRead();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _httpResponseMessage.RequestMessage.Dispose();
            _httpResponseMessage.Dispose();
        }
    }
}