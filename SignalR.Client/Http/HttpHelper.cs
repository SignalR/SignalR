using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
#if NET20
using System.Threading;
using SignalR.Client.Net20.Infrastructure;
#else
using System.Threading.Tasks;
#endif
using SignalR.Client.Infrastructure;
using SignalR.Infrastructure;

namespace SignalR.Client.Http
{
    internal static class HttpHelper
    {
#if NET20
        public static Task<HttpWebResponse> GetHttpResponseAsync(HttpWebRequest request)
        {
            var signal = new Task<HttpWebResponse>();
            try
            {
                request.BeginGetResponse(GetResponseCallback,
                                         new RequestState<HttpWebResponse> { Request = request, Response = signal });
            }
            catch (Exception ex)
            {
                signal.OnFinished(null,ex);
            }
            return signal;
        }

        public static Task<Stream> GetHttpRequestStreamAsync(HttpWebRequest request)
        {
            var signal = new Task<Stream>();
            try
            {
                request.BeginGetRequestStream(GetRequestStreamCallback,
                                         new RequestState<Stream> { Request = request, Response = signal });
            }
            catch (Exception ex)
            {
                signal.OnFinished(null, ex);
            }
            return signal;
        }


        private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            var requestState = (RequestState<Stream>)asynchronousResult.AsyncState;

            // End the operation
            try
            {
                var postStream = requestState.Request.EndGetRequestStream(asynchronousResult);

                // Write to the request stream.
                requestState.Response.OnFinished(postStream,null);
            }
            catch (WebException exception)
            {
                requestState.Response.OnFinished(null,exception);
            }
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            var requestState = (RequestState<HttpWebResponse>)asynchronousResult.AsyncState;

            // End the operation
            try
            {
                var response = (HttpWebResponse)requestState.Request.EndGetResponse(asynchronousResult);
                requestState.Response.OnFinished(response,null);
            }
            catch (Exception ex)
            {
                requestState.Response.OnFinished(null,ex);
            }
        }

#else
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
#endif

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
#if NET20
            return GetHttpResponseAsync(request);
#else
            return request.GetHttpResponseAsync();
#endif
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

#if NET20
        public static string ReadAsString(HttpWebResponse response)
#else
        public static string ReadAsString(this HttpWebResponse response)
#endif
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
#if NET20
                Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,"Failed to read response: {0}", ex));
#else
                Debug.WriteLine("Failed to read response: {0}", ex);
#endif
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
#if NET20
                return GetHttpResponseAsync(request);
#else
                return request.GetHttpResponseAsync();
#endif
            }

            // Write the post data to the request stream
#if NET20
            return GetHttpRequestStreamAsync(request)
                .Then(stream =>
                                {
                                    StreamExtensions.WriteAsync(stream, buffer);
                                    return stream;
                                }).Then(stream =>
                                                  {
                                                      stream.Dispose();
                                                      return 1;
                                                  }).Then(previousResult =>
                                                                    {
                                                                        var result =
                                                                            GetHttpResponseAsync(request);
                                                                        var resetEvent =
                                                                            new ManualResetEvent(false);
                                                                        HttpWebResponse response = null;
                                                                        result.OnFinish += (sender, e) =>
                                                                                               {
                                                                                                   response = e.ResultWrapper.Result;
                                                                                                   resetEvent.Set();
                                                                                               };

                                                                        resetEvent.WaitOne(TimeSpan.FromSeconds(20));
                                                                        return response;
                                                                    });
#else
            return (Task<HttpWebResponse>) request.GetHttpRequestStreamAsync()
                              .Then(stream => stream.WriteAsync(buffer).Then(() => stream.Dispose()))
                              .Then(() => request.GetHttpResponseAsync());
#endif
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

                sb.AppendFormat("{0}={1}", pair.Key, UriQueryUtility.UrlEncode(pair.Value));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }

    public class RequestState
    {
        public HttpWebRequest Request { get; set; }
    }

    public class RequestState<T> : RequestState
    {
        public Task<T> Response { get; set; }
    }
}