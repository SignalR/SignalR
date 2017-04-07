// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Web;

namespace Microsoft.AspNet.SignalR
{
    public static class RequestExtensions
    {
        /// <summary>
        /// Returns the <see cref="HttpContextBase"/> for this <see cref="IRequest"/>.
        /// </summary>
        /// <param name="request">The request</param>
        public static HttpContextBase GetHttpContext(this IRequest request)
        {
            object httpContextBaseValue;
            if (request.Environment.TryGetValue(typeof(HttpContextBase).FullName, out httpContextBaseValue))
            {
                return httpContextBaseValue as HttpContextBase;
            }

            return null;
        }
    }
}
