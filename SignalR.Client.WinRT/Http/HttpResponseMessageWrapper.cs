using System.IO;
using System.Net.Http;
using SignalR.Client.Http;

namespace SignalR.Client.WinRT.Http
{
    public class HttpResponseMessageWrapper : IResponse
    {
        private HttpResponseMessage _httpResponseMessage;

        public HttpResponseMessageWrapper(HttpResponseMessage httpResponseMessage)
        {
            _httpResponseMessage = httpResponseMessage;
        }

        public string ReadAsString()
        {
            return _httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

        public Stream GetResponseStream()
        {
            return _httpResponseMessage.Content.ReadAsStreamAsync().Result;
        }

        public void Close()
        {
            _httpResponseMessage.Dispose();
        }
    }
}
