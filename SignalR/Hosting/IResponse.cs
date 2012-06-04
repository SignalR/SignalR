using System.Threading.Tasks;

namespace SignalR
{
    public interface IResponse
    {
        bool IsClientConnected { get; }
        string ContentType { get; set; }

        Task WriteAsync(string data);
        Task EndAsync(string data);
    }
}
