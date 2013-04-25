// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Net;
using System.Net.Http;

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    /// Represents errors that are thrown by the SignalR client
    /// </summary>
    public class SignalRError : IDisposable
    {
        private HttpWebResponse _responseWeb;
        private HttpResponseMessage _responseMessage;

        /// <summary>
        /// Create custom SignalR based error.
        /// </summary>
        /// <param name="exception">The exception to unwrap</param>
        public SignalRError(Exception exception)
        {
            Exception = exception;
        }

        internal void SetHttpWebResponse(HttpWebResponse response)
        {
            _responseWeb = response;
        }

        internal void SetResponseMessage(HttpResponseMessage response)
        {
            _responseMessage = response;
        }

        /// <summary>
        /// The status code of the error (if it was a WebException)
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The response body of the error, if it was a WebException and the response is readable
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        /// The unwrapped underlying exception
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Allow a SignalRError to be directly written to an output stream
        /// </summary>
        /// <returns>Exception error</returns>
        public override string ToString()
        {
            return Exception.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_responseWeb != null)
                {
                    _responseWeb.Close();
                }

                if (_responseMessage != null)
                {
                    _responseMessage.Dispose();
                }
            }
        }

        /// <summary>
        /// Dispose of the response
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
