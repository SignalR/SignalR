using System.IO;

namespace SignalR.Client.Infrastructure
{
    public interface IHttpResponse
    {
        string ReadAsString();
        Stream GetResponseStream();
        void Close();
    }
}
