using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace SignalR.Server
{
    public class ServerResponse : IResponse
    {
        ResultParameters _resultParameters;
        readonly CallParameters _call;
        readonly TaskCompletionSource<ResultParameters> _resultParametersSource;
        readonly TaskCompletionSource<Stream> _outputStreamSource;
        readonly TaskCompletionSource<AsyncVoid> _responseEndSource;
        string _contentType;
        int _bufferingDisabled;
        bool _isClientConnected = true;

        struct AsyncVoid
        {
        }

        public ServerResponse(CallParameters call, TaskCompletionSource<ResultParameters> resultParametersSource)
        {
            _call = call;

            _resultParameters = new ResultParameters
            {
                Properties = new Dictionary<string, object>(),
                Status = 200,
                Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase),
                Body = OnResponseBody
            };

            _resultParametersSource = resultParametersSource;
            _outputStreamSource = new TaskCompletionSource<Stream>();
            _responseEndSource = new TaskCompletionSource<AsyncVoid>();
        }

        public void OnCallCompleted()
        {
            _isClientConnected = false;
            _resultParametersSource.TrySetCanceled();
            _outputStreamSource.TrySetCanceled();
            _responseEndSource.TrySetResult(default(AsyncVoid));
        }

        Task OnResponseBody(Stream output)
        {
            _outputStreamSource.TrySetResult(output);
            return _responseEndSource.Task;
        }

        public bool IsClientConnected
        {
            get { return _isClientConnected; }
        }

        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                _contentType = value;
                _resultParameters.Headers["Content-Type"] = new[] { value };
            }
        }

        public Task WriteAsync(ArraySegment<byte> data)
        {
            return DoStartAsync(disableBuffering: true)
                .Then(output => DoWriteAsync(output, data));
        }

        public Task EndAsync(ArraySegment<byte> data)
        {
            return DoStartAsync(disableBuffering: false)
                .Then(output => DoWriteAsync(output, data))
                .Finally(OnCallCompleted);
        }

        Task<Stream> DoStartAsync(bool disableBuffering)
        {
            if (disableBuffering && Interlocked.Increment(ref _bufferingDisabled) == 1)
            {
                DisableBuffering();
            }

            _resultParametersSource.TrySetResult(_resultParameters);
            return _outputStreamSource.Task;
        }

        Task DoWriteAsync(Stream output, ArraySegment<byte> data)
        {
            output.Write(data.Array, data.Offset, data.Count);
            #if NET45
                return output.FlushAsync();
            #endif
            output.Flush();
            return TaskHelpers.Completed();
        }

        void DisableBuffering()
        {
            object value;
            if (_call.Environment.TryGetValue("server.DisableRequestBuffering", out value))
            {
                var disableRequestBuffering = value as Action;
                if (disableRequestBuffering != null)
                {
                    disableRequestBuffering.Invoke();
                }
            }
            if (_call.Environment.TryGetValue("server.DisableResponseBuffering", out value))
            {
                var disableResponseBuffering = value as Action;
                if (disableResponseBuffering != null)
                {
                    disableResponseBuffering.Invoke();
                }
            }
        }
    }
}
