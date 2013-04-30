﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace Microsoft.AspNet.SignalR.Client
{
    [SuppressMessage("Microsoft.Design", "CA1032:Add constructor", Justification = "Silverlight does not support Serialization")]
    [SuppressMessage("Microsoft.Usage", "CA2237:Add serializable", Justification = "Silverlight does not support Serialization")]
    public class HttpClientException : Exception
    {
        public HttpResponseMessage Response { get; private set; }

        public HttpClientException()
            : base()
        {
        }

        public HttpClientException(string message)
            : base(message)
        {
        }

        public HttpClientException(string message, Exception ex)
            : base(message, ex)
        {
        }

        public HttpClientException(HttpResponseMessage responseMessage)
        {
            Response = responseMessage;
        }
    }
}
