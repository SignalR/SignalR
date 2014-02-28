// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
#if !NET4
using System.Net.Http;
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
            ex = ex.Unwrap();
            SignalRError error;

            var clientException = ex as WebException;

            if (clientException != null)
            {
                error = GetWebExceptionError(ex);
            }
#if !NET4
            else
            {
                error = GetHttpClientException(ex);
            }
#else
            else
            {
                error = new SignalRError(ex);
            }
#endif
            return error;
        }

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

#if !NET4
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
