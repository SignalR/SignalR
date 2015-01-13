// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    internal static class HttpHelper
    {
#if CLIENT_NET4

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync(request.BeginGetResponse, ar => (HttpWebResponse)request.EndGetResponse(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<HttpWebResponse>(ex);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<Stream> GetHttpRequestStreamAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<Stream>(ex);
            }
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            if (requestPreparer != null)
            {
                requestPreparer(request);
            }
            return request.GetHttpResponseAsync();
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            if (requestPreparer != null)
            {
                requestPreparer(request);
            }

            byte[] buffer = ProcessPostData(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            // Set the content length if the buffer is non-null
            request.ContentLength = buffer != null ? buffer.LongLength : 0;

            if (buffer == null)
            {
                // If there's nothing to be written to the request then just get the response
                return request.GetHttpResponseAsync();
            }

            // Write the post data to the request stream
            return request.GetHttpRequestStreamAsync()
                .Then(stream => stream.WriteAsync(buffer).Then(() => stream.Dispose()))
                .Then(() => request.GetHttpResponseAsync());
        }
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Text.StringBuilder.AppendFormat(System.String,System.Object[])", Justification = "This will never be localized.")]
        public static byte[] ProcessPostData(IDictionary<string, string> postData)
        {
            if (postData == null || postData.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var pair in postData)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }

                if (String.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                sb.AppendFormat("{0}={1}", pair.Key, UrlEncoder.UrlEncode(pair.Value));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
