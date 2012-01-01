using System.Threading.Tasks;
using System.Web;
using SignalR.Abstractions;

namespace SignalR.AspNet
{
    public class AspNetResponse : IResponse
    {
        private readonly HttpRequestBase _request;
        private readonly HttpResponseBase _response;

        public AspNetResponse(HttpRequestBase request, HttpResponseBase response)
        {
            _request = request;
            _response = response;
        }

        public bool Buffer
        {
            get
            {
                return _response.Buffer;
            }
            set
            {
                _response.Buffer = value;
                _response.BufferOutput = value;

                if (!value)
                {
                    // This forces the IIS compression module to leave this response alone.
                    // If we don't do this, it will buffer the response to suit its own compression
                    // logic, resulting in partial messages being sent to the client.
                    _request.Headers.Remove("Accept-Encoding");
                    _response.CacheControl = "no-cache";
                    _response.AddHeader("Connection", "keep-alive");
                }
            }
        }

        public bool IsClientConnected
        {
            get
            {
                return _response.IsClientConnected;
            }
        }

        public string ContentType
        {
            get
            {
                return _response.ContentType;
            }
            set
            {
                _response.ContentType = value;
            }
        }

        public Task WriteAsync(string data)
        {
            _response.Write(data);
            return TaskAsyncHelper.Empty;
        }
    }
}
