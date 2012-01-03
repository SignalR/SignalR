using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gate.Owin;
using SignalR.Abstractions;

namespace SignalR.Owin
{
    public class OwinResponse : IResponse
    {
        private ResultDelegate _responseCallback;
        private Func<ArraySegment<byte>, Action, bool> _responseNext;
        private Action<Exception> _responseError;
        private Action _responseCompete;

        public OwinResponse(ResultDelegate responseCallback)
        {
            _responseCallback = responseCallback;

            IsClientConnected = true;
            Buffer = true;
        }

        public bool Buffer
        {
            get;
            set;
        }

        public string ContentType
        {
            get;
            set;
        }

        public bool IsClientConnected
        {
            get;
            private set;
        }

        public Task WriteAsync(string data)
        {
            return EnsureResponseStarted()
                .Then(() => DoWrite(data))
                .Catch();
        }

        Task EnsureResponseStarted()
        {
            var responseCallback = Interlocked.Exchange(ref _responseCallback, null);
            if (responseCallback == null)
                return TaskAsyncHelper.Empty;

            var tcs = new TaskCompletionSource<object>();
            try
            {
                responseCallback(
                    "200 OK",
                    new Dictionary<string, string>
                        {
                            {"Content-Type", ContentType ?? "text/plain"},
                        },
                    (next, error, complete) =>
                    {
                        _responseNext = next;
                        _responseError = error;
                        _responseCompete = complete;
                        tcs.SetResult(null);
                        return StopSending;
                    });
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            return tcs.Task;
        }

        private void StopSending()
        {
            IsClientConnected = false;
        }

        private Task DoWrite(string data)
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                var value = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
                if (Buffer)
                {
                    // use Buffer==true to infer a single write and closed connection
                    
                    _responseNext(value, null);
                    _responseCompete();
                    tcs.SetResult(null);
                }
                else
                {
                    // use Buffer==true to infer an ongoing series of async writes that never end

                    if (!_responseNext(value, () => tcs.SetResult(null)))
                        tcs.SetResult(null);
                }
            }
            catch (Exception ex)
            {
                IsClientConnected = false;
                tcs.SetException(ex);
            }
            return tcs.Task;
        }
    }
}
