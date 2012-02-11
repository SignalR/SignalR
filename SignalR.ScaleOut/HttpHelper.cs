using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SignalR
{
    internal static class HttpHelper
    {
        public static Task<HttpWebResponse> GetResponseAsync(this HttpWebRequest request)
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

        public static Task<Stream> GetRequestStreamAsync(this HttpWebRequest request)
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

        public static Task<HttpWebResponse> GetAsync(string url)
        {
            return GetAsync(url, requestPreparer: null);
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            if (requestPreparer != null)
            {
                requestPreparer(request);
            }
            return request.GetResponseAsync();
        }

        public static Task<HttpWebResponse> PostAsync(string url)
        {
            return PostInternal(url, requestPreparer: null, postData: null);
        }

        public static Task<HttpWebResponse> PostAsync(string url, IDictionary<string, string> postData)
        {
            return PostInternal(url, requestPreparer: null, postData: postData);
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            return PostInternal(url, requestPreparer, postData: null);
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            return PostInternal(url, requestPreparer, postData);
        }

        public static string ReadAsString(this HttpWebResponse response)
        {
            try
            {
                using (response)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to read resonse: {0}", ex);
                // Swallow exceptions when reading the response stream and just try again.
                return null;
            }
        }

        private static Task<HttpWebResponse> PostInternal(string url, Action<HttpWebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);

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
                return request.GetResponseAsync();
            }

            // Write the post data to the request stream
            return request.GetRequestStreamAsync()
                .Then(stream => stream.WriteAsync(buffer).Then(() => stream.Close()))
                .Then(() => request.GetResponseAsync());
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

                sb.AppendFormat("{0}={1}", pair.Key, Uri.EscapeDataString(pair.Value));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static Task WriteAsync(this Stream stream, byte[] buffer)
        {
            try
            {
                return Task.Factory.FromAsync((cb, state) => stream.BeginWrite(buffer, 0, buffer.Length, cb, state), ar => stream.EndWrite(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
        }
    }
}