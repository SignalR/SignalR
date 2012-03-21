using System.IO;
using System.Net;

namespace SignalR.Client.Infrastructure
{
    public class HttpWebResponseWrapper : IResponse
    {
        private readonly HttpWebResponse _response;

        public HttpWebResponseWrapper(HttpWebResponse response)
        {
            _response = response;
        }

        public string ReadAsString()
        {
            return _response.ReadAsString();   
        }

        public Stream GetResponseStream()
        {
            return _response.GetResponseStream();
        }

        public void Close()
        {
            _response.Close();
        }
    }
}
