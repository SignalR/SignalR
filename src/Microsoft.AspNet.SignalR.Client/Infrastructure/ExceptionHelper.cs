// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
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

            // There is a race in StreamExtensions where if the endMethod in ReadAsync is called before
            // the Stream is disposed, but executes after, Stream.EndRead will be called on a disposed object.
            // Since we call HttpWebRequest.Abort in several places while potentially reading the stream,
            // and we don't want to lock around HttpWebRequest.Abort and Stream.EndRead, we just swallow the 
            // exception.
            // If the Stream is closed before the call to the endMethod, we expect an OperationCanceledException,
            // so this is a fairly rare race condition.
            if (exception is ObjectDisposedException)
            {
                return true;
            }

            var webException = exception as WebException;
            return (webException != null && webException.Status == WebExceptionStatus.RequestCanceled);
        }
    }
}
