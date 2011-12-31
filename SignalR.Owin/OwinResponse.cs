using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SignalR.Abstractions;

namespace SignalR.Owin
{
    using ResponseCallBack = System.Action<string, System.Collections.Generic.IDictionary<string, string>, System.Func<System.Func<System.ArraySegment<byte>, System.Action, bool>, System.Action<System.Exception>, System.Action, System.Action>>;
    
    public class OwinResponse : IResponse
    {
        private readonly ResponseCallBack _responseCallback;

        public OwinResponse(ResponseCallBack responseCallback)
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
            var tcs = new TaskCompletionSource<object>();
            var headers = new Dictionary<string, string> { { "Content-Type", ContentType } };

            _responseCallback.Invoke("200 OK", headers, (next, error, complete) => DoWrite(tcs, data, next, error, complete));

            return tcs.Task;
        }

        private Action DoWrite(TaskCompletionSource<object> tcs, string data, Func<ArraySegment<byte>, Action, bool> next, Action<Exception> error, Action complete)
        {
            try
            {
                var value = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
                next(value, null);
                complete();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                IsClientConnected = false;
                error(ex);
                tcs.SetException(ex);
            }

            return null;
        }
    }
}
