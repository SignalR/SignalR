using System;
using System.Net;

namespace SignalR.Client
{
    /// <summary>
    /// Represents errors that are thrown by the SignalR client
    /// </summary>
    public class SignalRError
    {
        public SignalRError(Exception exception)
        {
            Exception = exception;
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

        public override string ToString()
        {
            return Exception.ToString();
        }
    }
}
