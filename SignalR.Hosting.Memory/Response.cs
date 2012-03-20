using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Infrastructure;

namespace SignalR.Hosting.Memory
{
    public class Response : IHttpResponse, IResponse
    {
        private string _nonStreamingData;
        private readonly MemoryStream _stream = new MemoryStream();
        private readonly CancellationToken _clientToken;
        private bool _ended;

        public Response(CancellationToken clientToken)
        {
            _clientToken = clientToken;
        }

        public string ReadAsString()
        {
            return _nonStreamingData;
        }

        public Stream GetResponseStream()
        {
            return _stream;
        }

        public void Close()
        {
            _stream.Close();
            _ended = true;
        }

        public bool IsClientConnected
        {
            get
            {
                return !_ended && !_clientToken.IsCancellationRequested;
            }
        }

        public string ContentType
        {
            get;
            set;
        }

        public Task WriteAsync(string data)
        {
            if (!_ended)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                _stream.Write(bytes, 0, bytes.Length);
            }

            return TaskAsyncHelper.Empty;
        }

        public Task EndAsync(string data)
        {
            _nonStreamingData = data;
            return TaskAsyncHelper.Empty;
        }
    }
}
