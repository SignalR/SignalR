using System.IO;
using System.Net.Http;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class HttpResponseMessageWrapper : IResponse
    {
        private HttpResponseMessage _httpResponseMessage;
        private HttpClient _client;

        public HttpResponseMessageWrapper(HttpResponseMessage httpResponseMessage, HttpClient client)
        {
            _httpResponseMessage = httpResponseMessage;
            _client = client;
        }

        public string ReadAsString()
        {
            return _httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

        public Stream GetStream()
        {
            return _httpResponseMessage.Content.ReadAsStreamAsync().Result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpResponseMessage.Dispose();
                _client.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}