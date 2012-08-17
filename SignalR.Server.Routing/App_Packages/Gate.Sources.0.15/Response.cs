using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Gate.Utils;
using Owin;
using System.Text;
using System.IO;
using System.Threading;

namespace Gate
{
    internal class Response
    {
        private static readonly Encoding defaultEncoding = Encoding.UTF8;

        private ResultParameters result;
        private TaskCompletionSource<ResultParameters> callCompletionSource;
        private TaskCompletionSource<Response> sendHeaderAsyncCompletionSource;
        private TaskCompletionSource<object> bodyTransitionCompletionSource;
        private TaskCompletionSource<object> bodyCompletionSource;

        private CancellationToken completeToken;
        private ResponseStream responseStream;
        private Func<Stream, Task> defaultBodyDelegate;

        public Response()
            : this(200)
        {
        }

        public Response(int statusCode)
            : this(statusCode, null)
        {
        }

        public Response(int statusCode, IDictionary<string, string[]> headers)
            : this(statusCode, headers, null)
        {
        }

        public Response(int statusCode, IDictionary<string, string[]> headers, IDictionary<string, object> properties)
            : this(
                new ResultParameters()
                {
                    Status = statusCode,
                    Headers = headers,
                    Body = null,
                    Properties = properties
                })
        {
        }

        public Response(ResultParameters result, CancellationToken completed = default(CancellationToken))
        {
            this.defaultBodyDelegate = DefaultBodyDelegate;

            this.result.Status = result.Status;
            this.result.Body = result.Body ?? defaultBodyDelegate;
            this.result.Headers = result.Headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            this.result.Properties = result.Properties ?? new Dictionary<string, object>();

            this.callCompletionSource = new TaskCompletionSource<ResultParameters>();
            this.sendHeaderAsyncCompletionSource = new TaskCompletionSource<Response>();
            this.bodyTransitionCompletionSource = new TaskCompletionSource<object>();
            this.bodyCompletionSource = new TaskCompletionSource<object>();

            this.completeToken = completed;
            this.Encoding = defaultEncoding;
        }

        internal Func<Task<ResultParameters>> Next { get; set; }

        public void Skip()
        {
            Next.Invoke().CopyResultToCompletionSource(callCompletionSource);
        }

        public IDictionary<string, string[]> Headers
        {
            get { return result.Headers; }
            set { result.Headers = value; }
        }

        public IDictionary<string, object> Properties
        {
            get { return result.Properties; }
            set { result.Properties = value; }
        }

        public string Status
        {
            get
            {
                var reasonPhrase = ReasonPhrase;
                return string.IsNullOrEmpty(reasonPhrase)
                    ? StatusCode.ToString(CultureInfo.InvariantCulture)
                    : StatusCode.ToString(CultureInfo.InvariantCulture) + " " + reasonPhrase;
            }
            set
            {
                if (value.Length < 3 || (value.Length >= 4 && value[3] != ' '))
                {
                    throw new ArgumentException("Status must be a string with 3 digit statuscode, a space, and a reason phrase");
                }
                result.Status = int.Parse(value.Substring(0, 3));
                ReasonPhrase = value.Length < 4 ? null : value.Substring(4);
            }
        }

        public int StatusCode
        {
            get
            {
                return result.Status;
            }
            set
            {
                if (result.Status != value)
                {
                    result.Status = value;
                    ReasonPhrase = null;
                }
            }
        }

        public string ReasonPhrase
        {
            get
            {
                object value;
                var reasonPhrase = Properties.TryGetValue("owin.ReasonPhrase", out value) ? Convert.ToString(value) : null;
                return string.IsNullOrEmpty(reasonPhrase) ? ReasonPhrases.ToReasonPhrase(StatusCode) : reasonPhrase;
            }
            set { Properties["owin.ReasonPhrase"] = value; }
        }

        public string GetHeader(string name)
        {
            var values = GetHeaders(name);
            if (values == null)
            {
                return null;
            }

            switch (values.Length)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return values[0];
                default:
                    return string.Join(",", values);
            }
        }

        public string[] GetHeaders(string name)
        {
            string[] existingValues;
            return Headers.TryGetValue(name, out existingValues) ? existingValues : null;
        }

        public Response SetHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                Headers.Remove(value);
            else
                Headers[name] = new[] { value };
            return this;
        }

        public Response SetCookie(string key, string value)
        {
            Headers.AddHeader("Set-Cookie", Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "; path=/");
            return this;
        }

        public Response SetCookie(string key, Cookie cookie)
        {
            var domainHasValue = !string.IsNullOrEmpty(cookie.Domain);
            var pathHasValue = !string.IsNullOrEmpty(cookie.Path);
            var expiresHasValue = cookie.Expires.HasValue;

            var setCookieValue = string.Concat(
                Uri.EscapeDataString(key),
                "=",
                Uri.EscapeDataString(cookie.Value ?? ""), //TODO: concat complex value type with '&'?
                !domainHasValue ? null : "; domain=",
                !domainHasValue ? null : cookie.Domain,
                !pathHasValue ? null : "; path=",
                !pathHasValue ? null : cookie.Path,
                !expiresHasValue ? null : "; expires=",
                !expiresHasValue ? null : cookie.Expires.Value.ToString("ddd, dd-MMM-yyyy HH:mm:ss ") + "GMT",
                !cookie.Secure ? null : "; secure",
                !cookie.HttpOnly ? null : "; HttpOnly"
                );
            Headers.AddHeader("Set-Cookie", setCookieValue);
            return this;
        }

        public Response DeleteCookie(string key)
        {
            Func<string, bool> predicate = value => value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase);

            var deleteCookies = new[] { Uri.EscapeDataString(key) + "=; expires=Thu, 01-Jan-1970 00:00:00 GMT" };
            var existingValues = Headers.GetHeaders("Set-Cookie");
            if (existingValues == null)
            {
                Headers["Set-Cookie"] = deleteCookies;
                return this;
            }

            Headers["Set-Cookie"] = existingValues.Where(value => !predicate(value)).Concat(deleteCookies).ToArray();
            return this;
        }

        public Response DeleteCookie(string key, Cookie cookie)
        {
            var domainHasValue = !string.IsNullOrEmpty(cookie.Domain);
            var pathHasValue = !string.IsNullOrEmpty(cookie.Path);

            Func<string, bool> rejectPredicate;
            if (domainHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase) &&
                        value.IndexOf("domain=" + cookie.Domain, StringComparison.InvariantCultureIgnoreCase) != -1;
            }
            else if (pathHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase) &&
                        value.IndexOf("path=" + cookie.Path, StringComparison.InvariantCultureIgnoreCase) != -1;
            }
            else
            {
                rejectPredicate = value => value.StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase);
            }
            var existingValues = Headers.GetHeaders("Set-Cookie");
            if (existingValues != null)
            {
                Headers["Set-Cookie"] = existingValues.Where(value => !rejectPredicate(value)).ToArray();
            }

            return SetCookie(key, new Cookie
            {
                Path = cookie.Path,
                Domain = cookie.Domain,
                Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        }

        internal class Cookie
        {
            public Cookie()
            {
                Path = "/";
            }
            public Cookie(string value)
            {
                Path = "/";
                Value = value;
            }
            public string Value { get; set; }
            public string Domain { get; set; }
            public string Path { get; set; }
            public DateTime? Expires { get; set; }
            public bool Secure { get; set; }
            public bool HttpOnly { get; set; }
        }

        public string ContentType
        {
            get { return GetHeader("Content-Type"); }
            set { SetHeader("Content-Type", value); }
        }

        public long ContentLength
        {
            get { return long.Parse(GetHeader("Content-Length"), CultureInfo.InvariantCulture); }
            set { SetHeader("Content-Length", value.ToString(CultureInfo.InvariantCulture)); }
        }

        public Encoding Encoding { get; set; }

        // (true) Buffer or (false) auto-start/SendHeaders on write 
        public bool Buffer { get; set; }

        public Func<Stream, Task> BodyDelegate
        {
            get
            {
                return result.Body;
            }
            set
            {
                result.Body = value;
            }
        }

        private void EnsureResponseStream()
        {
            if (responseStream == null)
            {
                responseStream = new ResponseStream(this.completeToken);
                if (result.Body != defaultBodyDelegate)
                {
                    if (callCompletionSource.Task.IsCompleted)
                    {
                        throw new InvalidOperationException("The result has already been returned, the body delegate cannot be modified.");
                    }
                    result.Body = defaultBodyDelegate;
                }
            }
        }

        public Stream OutputStream
        {
            get
            {
                EnsureResponseStream();
                if (!Buffer) // Auto-Start
                {
                    Start();
                }
                return responseStream;
            }
        }

        public void Write(string text)
        {
            var data = (Encoding ?? defaultEncoding).GetBytes(text);
            Write(data);
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        public void Write(byte[] buffer)
        {
            completeToken.ThrowIfCancellationRequested();
            OutputStream.Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            completeToken.ThrowIfCancellationRequested();
            OutputStream.Write(buffer, offset, count);
        }

        public void Write(ArraySegment<byte> data)
        {
            completeToken.ThrowIfCancellationRequested();
            OutputStream.Write(data.Array, data.Offset, data.Count);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count)
        {
            return OutputStream.WriteAsync(buffer, offset, count, completeToken);
        }

        public Task WriteAsync(ArraySegment<byte> data)
        {
            return OutputStream.WriteAsync(data.Array, data.Offset, data.Count, completeToken);
        }

        public IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {

            return OutputStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public void EndWrite(IAsyncResult result)
        {
            OutputStream.EndWrite(result);
        }

        public void Flush()
        {
            OutputStream.Flush();
        }

        // Copy the buffer to the output and then provide direct access to the output stream.
        private Task DefaultBodyDelegate(Stream output)
        {
            EnsureResponseStream();
            Task transitionTask = responseStream.TransitionFromBufferedToUnbuffered(output)
                .Then(() =>
                {
                    // Offload complete
                    sendHeaderAsyncCompletionSource.TrySetResult(this);
                })
                .Catch(errorInfo =>
                {
                    sendHeaderAsyncCompletionSource.TrySetCanceled();
                    bodyCompletionSource.TrySetException(errorInfo.Exception);
                    return errorInfo.Handled();
                })
                .Finally(() =>
                {
                    bodyTransitionCompletionSource.TrySetResult(null);
                });

            return bodyCompletionSource.Task;
        }

        public void Start()
        {
            StartAsync();
        }

        // Finalizes the status/headers/properties and returns a Task to notify the caller when the 
        // BodyDelegate has been invoked, the buffered data offloaded, and it is now safe to write unbuffered data.
        public Task<Response> StartAsync()
        {
            // Only execute once.
            if (!callCompletionSource.Task.IsCompleted)
            {
                callCompletionSource.TrySetResult(result);

                // TODO: Make Headers and Properties read only to prevent user errors

                if (result.Body == defaultBodyDelegate)
                {
                    // Register for cancelation in case the body delegate is not invoked.
                    completeToken.Register(() => sendHeaderAsyncCompletionSource.TrySetCanceled());
                }
                else
                {
                    // Not using the default body delegate, don't block.
                    sendHeaderAsyncCompletionSource.TrySetResult(this);
                    bodyTransitionCompletionSource.TrySetResult(null);
                }
            }
            return sendHeaderAsyncCompletionSource.Task;
        }

        public ResultParameters Result
        {
            get { return result; }
        }

        public Task<ResultParameters> ResultTask
        {
            get { return callCompletionSource.Task; }
        }

        public void End()
        {
            EndAsync();
        }

        // We are completely done with the response and body.
        public Task<ResultParameters> EndAsync()
        {
            Start();
            sendHeaderAsyncCompletionSource.TrySetCanceled();
            // End the body as soon as the buffer copies.
            bodyTransitionCompletionSource.Task.Then(() => { bodyCompletionSource.TrySetResult(null); });
            return callCompletionSource.Task;
        }

        public void Error(Exception error)
        {
            callCompletionSource.TrySetException(error);
            // This just goes back to user code, we don't need to report their own exception back to them.
            sendHeaderAsyncCompletionSource.TrySetCanceled();
            bodyCompletionSource.TrySetException(error);
        }
    }
}