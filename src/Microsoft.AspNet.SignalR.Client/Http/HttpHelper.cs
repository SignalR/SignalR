﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync<HttpWebResponse>(request.BeginGetResponse, ar => (HttpWebResponse)request.EndGetResponse(ar), null);
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
            HttpWebRequest request = CreateWebRequest(url);
            if (requestPreparer != null)
            {
                requestPreparer(request);
            }
            return request.GetHttpResponseAsync();
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            return PostInternal(url, requestPreparer, postData);
        }


        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Callers check for null return.")]
        public static string ReadAsString(this HttpWebResponse response)
        {
            try
            {
                using (response)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(stream);

                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
#if NET35
                Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to read response: {0}", ex));
#else
                Debug.WriteLine("Failed to read response: {0}", ex);
#endif
                // Swallow exceptions when reading the response stream and just try again.
                return null;
            }
        }

        private static Task<HttpWebResponse> PostInternal(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            HttpWebRequest request = CreateWebRequest(url);

            if (requestPreparer != null)
            {
                requestPreparer(request);
            }

            byte[] buffer = ProcessPostData(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
#if !WINDOWS_PHONE && !SILVERLIGHT
            // Set the content length if the buffer is non-null
            request.ContentLength = buffer != null ? buffer.LongLength : 0;
#endif

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

        private static byte[] ProcessPostData(IDictionary<string, string> postData)
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

        private static HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest request = null;
#if WINDOWS_PHONE
            request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowReadStreamBuffering = false;
#elif SILVERLIGHT
            request = (HttpWebRequest)System.Net.Browser.WebRequestCreator.ClientHttp.Create(new Uri(url));
            request.AllowReadStreamBuffering = false;
#else
            request = (HttpWebRequest)WebRequest.Create(url);
#endif
            return request;
        }
    }
}
