// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet
{
    internal static class HttpResponseExtensions
    {
        public static Task FlushAsync(this HttpResponseBase response)
        {
            if (!response.IsClientConnected)
            {
                return TaskAsyncHelper.Empty;
            }
#if NET45
            if (response.SupportsAsyncFlush)
            {
                return TaskAsyncHelper.FromAsync((cb, state) => response.BeginFlush(cb, state), ar => response.EndFlush(ar), null);
            }      
#endif
            try
            {
                response.Flush();
                return TaskAsyncHelper.Empty;
            }
            catch(Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
        }
    }
}
