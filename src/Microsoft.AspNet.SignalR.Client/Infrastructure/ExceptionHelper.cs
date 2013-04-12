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

            var webException = exception as WebException;
            return (webException != null && webException.Status == WebExceptionStatus.RequestCanceled);
        }
    }
}
