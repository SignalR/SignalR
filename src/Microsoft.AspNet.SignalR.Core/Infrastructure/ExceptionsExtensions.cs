// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal static class ExceptionsExtensions
    {
        internal static Exception Unwrap(this Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            var next = ex.GetBaseException();
            while (next.InnerException != null)
            {
                // On mono GetBaseException() doesn't seem to do anything
                // so just walk the inner exception chain.
                next = next.InnerException;
            }

            return next;
        }
    }
}

