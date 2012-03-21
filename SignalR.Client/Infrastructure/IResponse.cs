using System.IO;

namespace SignalR.Client.Infrastructure
{
    public interface IResponse
    {
        string ReadAsString();
        Stream GetResponseStream();
        void Close();
    }
}
