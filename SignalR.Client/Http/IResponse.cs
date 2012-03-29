using System.IO;

namespace SignalR.Client.Http
{
    public interface IResponse
    {
        string ReadAsString();
        Stream GetResponseStream();
        void Close();
    }
}
