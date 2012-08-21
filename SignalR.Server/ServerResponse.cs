using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Owin;

namespace SignalR.Server
{
    class ServerResponse : IResponse
    {
        readonly Task _completed;
        ResultParameters _resultParameters;

        readonly TaskCompletionSource<ResultParameters> _resultParametersSource;
        readonly TaskCompletionSource<Stream> _responseStreamSource;
        readonly TaskCompletionSource<AsyncVoid> _responseEndSource;
        readonly ServerResponseStream _outputStream;
        string _contentType;

        struct AsyncVoid
        {
        }

        internal ServerResponse(
            Task completed, 
            TaskCompletionSource<ResultParameters> resultParametersSource)
        {
            _completed = completed;

            _resultParameters = new ResultParameters
            {
                Properties = new Dictionary<string, object>(),
                Status = 200,
                Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase),
                Body = OnResponseBody
            };

            _resultParametersSource = resultParametersSource;
            _responseStreamSource = new TaskCompletionSource<Stream>();
            _responseEndSource = new TaskCompletionSource<AsyncVoid>();
            _outputStream = new ServerResponseStream(this);

            _completed.Finally(OnCallCompleted, runSynchronously: true);
        }

        void OnCallCompleted()
        {
            _resultParametersSource.TrySetCanceled();
            _responseStreamSource.TrySetCanceled();
            _responseEndSource.TrySetResult(default(AsyncVoid));
        }

        public Task<Stream> StartAsync()
        {
            if (!_resultParametersSource.Task.IsCompleted)
            {
                _resultParametersSource.TrySetResult(_resultParameters);
            }
            return _responseStreamSource.Task;
        }

        public void End()
        {
            StartAsync().Finally(
                () => _responseEndSource.TrySetResult(default(AsyncVoid)), 
                runSynchronously: true);
        }

        Task OnResponseBody(Stream output)
        {
            _responseStreamSource.TrySetResult(output);
            return _responseEndSource.Task;
        }

        public bool IsClientConnected
        {
            get { return !_completed.IsCompleted; }
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

        public IDictionary<string,string[]> Headers
        {
            get { return _resultParameters.Headers; }
        }

        public Stream OutputStream
        {
            get { return _outputStream; }
        }


    }
}
