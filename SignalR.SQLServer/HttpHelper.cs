using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SignalR {
    public static class HttpHelper {
        public static Task<HttpWebResponse> PostAsync(string url) {
            return PostInternal(url, _ => { }, new Dictionary<string, string>());
        }

        public static Task<HttpWebResponse> PostAsync(string url, IDictionary<string, string> postData) {
            return PostInternal(url, _ => { }, postData);
        }

        public static Task<HttpWebResponse> PostAsync(string url, Action<WebRequest> requestPreparer, IDictionary<string, string> postData) {
            return PostInternal(url, requestPreparer, postData);
        }

        public static string ReadAsString(this HttpWebResponse response) {
            using (response) {
                using (Stream stream = response.GetResponseStream()) {
                    using (var reader = new StreamReader(stream)) {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        private static Task<string> ReadAsync(this Stream stream) {
            var tcs = new TaskCompletionSource<string>();
            var sb = new StringBuilder(4096);
            ReadAsync(sb, stream, tcs);
            return tcs.Task;
        }

        private static Task<HttpWebResponse> PostInternal(string url, Action<WebRequest> requestPreparer, IDictionary<string, string> postData) {
            var tcs = new TaskCompletionSource<HttpWebResponse>();
            var request = (HttpWebRequest)HttpWebRequest.Create(url);

            requestPreparer(request);

            var sb = new StringBuilder();
            foreach (var pair in postData) {
                if (sb.Length > 0) {
                    sb.Append("&");
                }

                if (String.IsNullOrEmpty(pair.Value)) {
                    continue;
                }

                sb.AppendFormat("{0}={1}", pair.Key, Uri.EscapeUriString(pair.Value));
            }

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = buffer.LongLength;

            request.BeginGetRequestStream(ar => {
                try {
                    using (Stream requestStream = request.EndGetRequestStream(ar)) {
                        requestStream.Write(buffer, 0, buffer.Length);
                    }
                }
                catch (Exception ex) {
                    tcs.SetException(ex);
                    return;
                }

                request.BeginGetResponse(iar => {
                    try {
                        tcs.SetResult((HttpWebResponse)request.EndGetResponse(iar));
                    }
                    catch (Exception ex) {
                        tcs.SetException(ex);
                    }
                }, null);

            }, null);

            return tcs.Task;
        }

        private static void ReadAsync(StringBuilder sb, Stream stream, TaskCompletionSource<string> tcs) {
            byte[] buffer = new byte[1024 * 4];

            stream.BeginRead(buffer, 0, buffer.Length - 1, ar => {
                int read = stream.EndRead(ar);
                sb.Append(Encoding.UTF8.GetString(buffer, 0, read));

                if (read < buffer.Length) {
                    tcs.SetResult(sb.ToString());
                }
                else {
                    ReadAsync(sb, stream, tcs);
                }
            }, null);
        }
    }
}