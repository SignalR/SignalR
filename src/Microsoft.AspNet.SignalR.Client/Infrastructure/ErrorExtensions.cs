// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;

#if NETSTANDARD1_3 || NETSTANDARD2_0 || NET45
using System.Net.Http;
#elif NET40
// Not needed
#else 
#error Unsupported target framework.
#endif

using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client
{
    public static class ErrorExtensions
    {
        /// <summary>
        /// Simplifies error recognition by unwrapping complex exceptions.
        /// </summary>
        /// <param name="ex">The thrown exception.</param>
        /// <returns>An unwrapped exception in the form of a SignalRError.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The IDisposable object is the return value.")]
        public static SignalRError GetError(this Exception ex)
        {
            // PORT: This logic was simplified when unifying the build across all frameworks
            ex = ex.Unwrap();

#if NET40 || NET45
            if (ex is WebException webEx)
            {
                return GetWebExceptionError(webEx);
            }
#elif NETSTANDARD1_3 || NETSTANDARD2_0
// Not supported on this framework
#else
#error Unsupported framework.
#endif

#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0
            if(ex is HttpClientException httpClientEx)
            {
                return GetHttpClientException(httpClientEx);
            }
#elif NET40
// Not supported on this framework
#else
#error Unsupported framework.
#endif

            return new SignalRError(ex);
        }

#if NET40 || NET45
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The IDisposable object is the return value.")]
        private static SignalRError GetWebExceptionError(Exception ex)
        {
            var error = new SignalRError(ex);
            var wex = ex as WebException;

            if (wex != null && wex.Response != null)
            {
                var response = wex.Response as HttpWebResponse;

                if (response != null)
                {
                    error.SetResponse(response);
                    error.StatusCode = response.StatusCode;
                    Stream originStream = response.GetResponseStream();

                    if (originStream.CanRead)
                    {
                        // We need to copy the stream over and not consume it all on "ReadToEnd".  If we consumed the entire stream GetError
                        // would only be able to be called once per Exception, otherwise you get inconsistent ResponseBody results.
                        Stream stream = Clone(originStream);

                        // Consume our copied stream
                        using (var sr = new StreamReader(stream))
                        {
                            error.ResponseBody = sr.ReadToEnd();
                        }
                    }
                }
            }

            return error;
        }
#elif NETSTANDARD1_3 || NETSTANDARD2_0
// Not supported on this framework
#else
#error Unsupported framework.
#endif

#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The IDisposable object is the return value.")]
        private static SignalRError GetHttpClientException(Exception ex)
        {
            var error = new SignalRError(ex);
            var hex = ex as HttpClientException;

            if (hex != null && hex.Response != null)
            {
                var response = hex.Response as HttpResponseMessage;

                if (response != null)
                {
                    error.SetResponse(response);
                    error.StatusCode = response.StatusCode;
                    error.ResponseBody = response.Content.ReadAsStringAsync().Result;
                }
            }

            return error;
        }
#elif NET40
        // Not supported on this framework.
#else
#error Unsupported framework.
#endif

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The return value of this private method is disposed in GetError.")]
        private static Stream Clone(Stream source)
        {
            var cloned = new MemoryStream();
            source.CopyTo(cloned);

            // Move the stream pointers back to the original start locations
            if (source.CanSeek)
            {
                source.Seek(0, 0);
            }

            cloned.Seek(0, 0);

            return cloned;
        }

    }
}
