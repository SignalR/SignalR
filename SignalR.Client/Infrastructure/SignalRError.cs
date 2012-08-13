﻿using System;
using System.Net;

namespace SignalR.Client
{
    /// <summary>
    /// Represents errors that are thrown by the SignalR client
    /// </summary>
    public class SignalRError : IDisposable
    {
        private HttpWebResponse _response;

        /// <summary>
        /// Create custom SignalR based error.
        /// </summary>
        /// <param name="exception">The exception to unwrap</param>
        public SignalRError(Exception exception)
        {
            Exception = exception;
        }

        /// <summary>
        /// Capture the response so we can dispose of it later
        /// </summary>
        /// <param name="response">The HttpWebResponse associated with the Exception, assuming its a WebException</param>
        public void SetResponse(HttpWebResponse response)
        {
            _response = response;
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

        /// <summary>
        /// Dispose of the response
        /// </summary>
        public void Dispose()
        {
            if (_response != null)
            {
                _response.Close();
            }
        }
    }
}
