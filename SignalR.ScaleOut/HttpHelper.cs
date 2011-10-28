using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SignalR
{
    internal static class HttpHelper
    {
        public static Task<HttpWebResponse> GetAsync(this HttpWebRequest request)
        {
            return Task.Factory.FromAsync<HttpWebResponse>(request.BeginGetResponse, iar => (HttpWebResponse)request.EndGetResponse(iar), null);
        }

        public static Task<Stream> GetRequestStreamAsync(this HttpWebRequest request)
        {
            return Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null);
        }

        public static Task<HttpWebResponse> GetAsync(string url)
        {
            return GetAsync(url, _ => { });
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            requestPreparer(request);
            return request.GetAsync();
        }

        public static Task<HttpWebResponse> PostAsync(string url)
        {
            return PostInternal(url, _ => { }, new Dictionary<string, string>());
        }

        public static Task<HttpWebResponse> PostAsync(string url, IDictionary<string, string> postData)
        {
            return PostInternal(url, _ => { }, postData);
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<WebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            return PostInternal(url, requestPreparer, postData);
        }

        public static string ReadAsString(this HttpWebResponse response)
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

        private static Task<HttpWebResponse> PostInternal(string url, Action<WebRequest> requestPreparer, IDictionary<string, string> postData)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);

            requestPreparer(request);

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

                sb.AppendFormat("{0}={1}", pair.Key, Uri.EscapeUriString(pair.Value));
            }

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
#if !WINDOWS_PHONE
            request.ContentLength = buffer.LongLength;
#endif

            return request.GetRequestStreamAsync()
                .Success(t =>
                {
                    t.Result.Write(buffer, 0, buffer.Length);
#if WINDOWS_PHONE
                    t.Result.Close();
#endif
                })
                .Success(t =>
                {
                    return request.GetAsync();
                })
                .Unwrap();
        }
    }
}