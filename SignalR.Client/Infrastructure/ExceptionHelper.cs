using System;
using System.Net;

namespace SignalR.Client.Infrastructure
{
    internal static class ExceptionHelper
    {
        internal static bool IsRequestAborted(Exception exception)
        {
            exception = exception.Unwrap();

            // Support an alternative way to propagate aborted requests
            if (exception is OperationCanceledException)
            {
                return true;
            }

            var webException = exception as WebException;
            return (webException != null && webException.Status == WebExceptionStatus.RequestCanceled);
        }
    }
}
